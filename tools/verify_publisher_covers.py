"""Verify cached product evidence and reachable cover headers, optionally following collection.
Does not save cover images or write database records. Saves per-URL check results
and a manifest containing only accepted bibliographic records.
"""
import argparse, hashlib, io, json, re, sys, time, urllib.request
import threading
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timezone, timedelta
from pathlib import Path
from urllib.parse import urlparse, quote
sys.path.insert(0,str(Path('artifacts/novel-publisher-import/python-deps').resolve()))
from bs4 import BeautifulSoup
from PIL import Image
sys.stdout.reconfigure(encoding='utf-8')
parser=argparse.ArgumentParser()
parser.add_argument('directory')
parser.add_argument('--follow-log')
parser.add_argument('--input',default='publisher-candidates.json')
args=parser.parse_args()
root=Path(args.directory)
review_path=Path('data/curation/publisher-form-reviews-v1.json')
reviews={e['SourceUrl']:e for e in json.loads(review_path.read_text(encoding='utf-8-sig'))} if review_path.exists() else {}
checks_path=root/'cover-checks.json'
checks=json.loads(checks_path.read_text(encoding='utf-8')) if checks_path.exists() else {}
checks={url:result for url,result in checks.items() if result.get('Reason')!='could not create decoder object'
    and "'ascii' codec can't encode" not in result.get('Reason','')
    and datetime.fromisoformat(result['CheckedAtUtc'])>datetime.now(timezone.utc)-timedelta(hours=24)}
rate_lock=threading.Lock()
save_lock=threading.Lock()
last_request=0.0
allowed={'www.dogankitap.com.tr','img.iskultur.com.tr','wwwithakiyayingrubucom.sm.mncdn.com'}
class SafeRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self,req,fp,code,msg,headers,newurl):
        if urlparse(newurl).scheme!='https' or urlparse(newurl).hostname not in allowed:
            raise ValueError('Cover redirect outside publisher image hosts')
        return super().redirect_request(req,fp,code,msg,headers,newurl)
opener=urllib.request.build_opener(SafeRedirect())

def verify_cover(url):
    global last_request
    if url in checks: return checks[url]['Ok']
    try:
        if urlparse(url).scheme!='https' or urlparse(url).hostname not in allowed:
            raise ValueError('Unsupported cover host')
        with rate_lock:
            time.sleep(max(0,1-(time.monotonic()-last_request)))
            last_request=time.monotonic()
        req=urllib.request.Request(quote(url,safe=':/?=&%'),headers={'User-Agent':'FavoBooks-CatalogResearch/1.0','Range':'bytes=0-65535'})
        with opener.open(req,timeout=30) as response:
            mime=response.headers.get_content_type()
            if not mime.startswith('image/'): raise ValueError('Response is not an image')
            data=response.read(65536)
        # WebP decoders may require the complete body even just to open a file.
        # Read the documented RIFF dimension fields instead of downloading covers.
        if data[:4]==b'RIFF' and data[8:12]==b'WEBP':
            tag=data[12:16]
            if tag==b'VP8X':width,height=1+int.from_bytes(data[24:27],'little'),1+int.from_bytes(data[27:30],'little')
            elif tag==b'VP8L' and data[20]==0x2f:
                bits=int.from_bytes(data[21:25],'little');width,height=(bits&0x3fff)+1,((bits>>14)&0x3fff)+1
            elif tag==b'VP8 ' and data[23:26]==b'\x9d\x01\x2a':
                width,height=int.from_bytes(data[26:28],'little')&0x3fff,int.from_bytes(data[28:30],'little')&0x3fff
            else:raise ValueError('Unsupported WebP header')
        else:
            with Image.open(io.BytesIO(data)) as image:
                width,height=image.size
        if width<80 or height<100: raise ValueError('Cover too small or placeholder')
        result=dict(Ok=True,Width=width,Height=height,Mime=mime,CheckedAtUtc=datetime.now(timezone.utc).isoformat())
    except Exception as e:
        result=dict(Ok=False,Reason=str(e)[:160],CheckedAtUtc=datetime.now(timezone.utc).isoformat())
    with save_lock:
        checks[url]=result
        temporary=checks_path.with_suffix('.tmp')
        temporary.write_text(json.dumps(checks,ensure_ascii=False,indent=2),encoding='utf-8')
        temporary.replace(checks_path)
    return result['Ok']

while True:
    try: entries=json.loads((root/args.input).read_text(encoding='utf-8'))
    except (FileNotFoundError,ValueError):
        time.sleep(5);continue
    accepted=[];rejected=[]
    for e in entries:
        try:
            review=reviews.get(e['SourceUrl'])
            if review and (review['SourceSha256']!=e['SourceSha256'] or review['Title']!=e['Title']):
                raise ValueError('Editorial review does not match source identity')
            if review and review['Decision']=='Hold':raise ValueError(review['Reason'])
            reviewed=bool(review and review['Decision']=='Novel')
            if reviewed:e['ReviewNote']=review['Reason']
            key=hashlib.sha256(e['SourceUrl'].encode()).hexdigest()
            source_path=root/'pages'/(key+'.html')
            raw=source_path.read_bytes().decode('utf-8')
            # Text-mode caches created on Windows may add one CR before each LF.
            # Match the recorded digest; never silently accept a changed source.
            body=next((b for b in (raw,raw.replace('\r\n','\n'),source_path.read_text(encoding='utf-8'))
                if hashlib.sha256(b.encode()).hexdigest()==e['SourceSha256']),None)
            if body is None:
                raise ValueError('Source content hash mismatch')
            soup=BeautifulSoup(body,'html.parser')
            description=''
            if 'dogankitap' in e['SourceUrl']:
                section=soup.select_one('.kd-aciklama')
                description=section.get_text(' ',strip=True) if section else ''
                if len(description)<30:
                    raise ValueError('Missing descriptive evidence; list category alone is insufficient')
                heading=soup.select_one('.kd-baslik')
                if heading:description=heading.get_text(' ',strip=True)+' '+description
                label=''
                for script in soup.select('script[type="application/ld+json"]'):
                    try:
                        product=json.loads(script.string or script.get_text())
                        if product.get('@type')=='Book':label=product.get('publisher',{}).get('name','').strip()
                    except (ValueError,AttributeError):pass
                if not label:
                    list_key=hashlib.sha256(e['ListUrl'].encode()).hexdigest()
                    listing=BeautifulSoup((root/'pages'/(list_key+'.html')).read_text(encoding='utf-8'),'html.parser')
                    anchor=listing.select_one('a.kitapkare[href="'+urlparse(e['SourceUrl']).path+'"]')
                    imprint=anchor.select_one('.k-firma') if anchor else None
                    label=imprint.get_text(' ',strip=True) if imprint else ''
                imprints={'dk':'Doğan Kitap','doğan kitap':'Doğan Kitap','novus':'Doğan Novus',
                    'doğan novus':'Doğan Novus','doğan solibri':'Doğan SoLibri'}
                canonical=imprints.get(label.casefold())
                if not canonical or (canonical!='Doğan Kitap' and not reviewed):
                    raise ValueError('Publisher imprint requires separate review: '+label)
                e['Publisher']=canonical
            elif 'ithakiyayingrubu' in e['SourceUrl']:
                section=soup.select_one('.product__description')
                description=section.get_text(' ',strip=True) if section else ''
            else:
                for s in soup.select('script[type="application/ld+json"]'):
                    try:
                        d=json.loads(s.string or s.get_text())
                        for x in d.get('@graph',[d]):
                            if x.get('@type')=='Product':description=x.get('description','')
                    except (ValueError,AttributeError):pass
                section=soup.select_one('.text-underline.lineH20')
                if section:description=section.get_text(' ',strip=True)
            if not reviewed and re.search(r'\b(özyaşamöyküsü|otobiyografi|anı kitabı|hatıralar|günlükler|mektuplar)\b',description,re.I):
                raise ValueError('Autobiography, memoir, diary or letters require review')
            if not reviewed and re.search(r'\b\d+\s+(öykü|hik[aâ]ye)\w*\s+(bir araya|oluş)',description,re.I):
                raise ValueError('Explicit short-story collection')
            if not reviewed and re.search(r'\b(öykü (kitabı|derlemesi)|hikâye (kitabı|derlemesi)|şiir (kitabı|derlemesi)|deneme (kitabı|derlemesi)|novella|roman olmayan)\b',description,re.I):
                raise ValueError('Conflicting literary form in publisher description')
            if not reviewed and re.search(r'\b(öykülerden|hikâyelerden|denemelerden|şiirlerden)\s+(oluş|derle)|\bbir\s+(biyografi|otobiyografi|anı kitabı)\b',description,re.I):
                raise ValueError('Collection or nonfiction form in publisher description')
            if not reviewed and re.search(r'\b(uzun öykü|öyküler|öyküleri|hikâyeler|hikâyeleri|şiirler|şiirleri|denemeler|denemeleri)\b',description,re.I):
                raise ValueError('Ambiguous story/poetry/essay form requires review')
            if not reviewed and re.search(r'\b(şiir|rubai|denemeler|anılar|anı kitabı|öyküler|öyküleri|hikâyeler|hikayeler|seçme öykü|toplu öykü|bütün öykü|toplu şiir|bütün şiir)\b',e['Title'],re.I):
                raise ValueError('Excluded literary form in title')
            accepted.append(e)
        except Exception as error:rejected.append(dict(SourceUrl=e['SourceUrl'],Title=e['Title'],Reason=str(error)))
        if (len(accepted)+len(rejected))%20==0:
            print('COVERS',len(accepted)+len(rejected),'/',len(entries),'accepted',len(accepted),'rejected',len(rejected),flush=True)
    with ThreadPoolExecutor(max_workers=4) as pool:
        for count,_ in enumerate(pool.map(verify_cover,dict.fromkeys(e['CoverImageUrl'] for e in accepted)),1):
            if count%50==0:print('IMAGE HEADERS',count,'/',len(accepted),flush=True)
    covered=[]
    for e in accepted:
        if checks[e['CoverImageUrl']]['Ok']:covered.append(e)
        else:rejected.append(dict(SourceUrl=e['SourceUrl'],Title=e['Title'],Reason='Cover verification failed'))
    accepted=covered
    payload=json.dumps(accepted,ensure_ascii=False,indent=2).encode('utf-8')
    (root/'verified-publisher-novels.json').write_bytes(payload)
    (root/'verification-rejections.json').write_text(json.dumps(rejected,ensure_ascii=False,indent=2),encoding='utf-8')
    (root/'verification-summary.json').write_text(json.dumps(dict(ManifestSha256=hashlib.sha256(payload).hexdigest(),
        Accepted=len(accepted),Rejected=len(rejected),CheckedAtUtc=min(
            [datetime.now(timezone.utc)]+[datetime.fromisoformat(checks[e['CoverImageUrl']]['CheckedAtUtc']) for e in accepted]).isoformat(),
        EditorialReviewsSha256=hashlib.sha256(review_path.read_bytes()).hexdigest() if review_path.exists() else None),indent=2),encoding='utf-8')
    if not args.follow_log:break
    log=Path(args.follow_log).read_text(encoding='utf-8-sig',errors='replace')
    latest=json.loads((root/args.input).read_text(encoding='utf-8'))
    if 'DONE ' in log and len(latest)==len(entries):break
    print('Waiting for collected products; verified',len(accepted),flush=True)
    time.sleep(10)
print('DONE verified',len(accepted),flush=True)
