"""Prepare candidate reports for human review; never writes to the database."""
import json,re,sys,unicodedata
from pathlib import Path
sys.stdout.reconfigure(encoding='utf-8')
batch=sys.argv[1]
if not re.fullmatch('[a-z0-9-]+',batch):raise ValueError('Invalid batch')
data=Path('data/curation')/batch;art=Path('artifacts')/batch
def read(path,default):return json.loads(path.read_text(encoding='utf-8-sig')) if path.exists() else default
def key(s):return ''.join(c for c in unicodedata.normalize('NFKD',s.lower().replace('ı','i')) if c.isalnum())
def titlekey(s):return key(re.split(r'\s+[–—-]\s+|\s*\(',s)[0])
config=read(data/'review-config.json',{})
aliases={int(k):v for k,v in config.get('aliases',{}).items()}
duplicates={int(k):v for k,v in config.get('duplicates',{}).items()}
catalog=read(art/'catalog.json',[])
index={}
for b in catalog:
    for k in {titlekey(t) for t in [b['Title']]+b['Editions']}:index.setdefault(k,[]).append(b)
search={r['Number']:r for r in read(art/'editions/search.json',[])}
preferred=['TÜRKİYE İŞ BANKASI','YAPI KREDİ','CAN YAYINLARI','İLETİŞİM','METİS','İTHAKİ','ALTIN KİTAPLAR','ALFA','DOĞAN','KORİDOR']
out=[];matches=[]
for n,line in enumerate((data/'requested.txt').read_text(encoding='utf-8').splitlines(),1):
    if n in duplicates:continue
    title,author=map(str.strip,line.split('|'));keys={titlekey(t) for t in [title]+aliases.get(n,[])}
    def sameauthor(a):
        parts=author.split();surname=key(parts[-1])
        return (surname in key(a) or any(key(x) in key(a) for x in config.get('authorAliases',{}).get(str(n),[]))) and (surname!='bronte' or key(parts[0]) in key(a))
    choices=[c for c in search.get(n,{}).get('Candidates',[]) if sameauthor(c['Author']) and not re.search(r'kısalt|İngilizce|English|Fransızca|Çizgi\b|Sticker|özeti|defter|inceleme',c['Title'],re.I)]
    choices.sort(key=lambda c:(titlekey(c['Title']) not in keys,next((i for i,p in enumerate(preferred) if p in c['Publisher']),99)))
    override=config.get('editionUrls',{}).get(str(n))
    if override:
        choices=[next((c for r in search.values() for c in r['Candidates'] if c['Url']==override),dict(Title=config.get('titles',{}).get(str(n),title),Author=author,Publisher='',Url=override))]
    existing=list({b['BookId']:b for k in keys for b in index.get(k,[])}.values())
    matches.append(dict(Number=n,Title=title,Author=author,Matches=existing))
    if choices:
        c=choices[0];out.append(dict(Number=n,RequestedTitle=title,RequestedAuthor=author,**c))
        print(n,'EXACT' if titlekey(c['Title']) in keys else 'REVIEW',c['Title'],'|',c['Author'],'|',c['Publisher'])
    else:print(n,'MISSING',title)
(art/'selection-candidates.json').write_text(json.dumps(out,ensure_ascii=False,indent=2),encoding='utf-8')
(art/'matches.json').write_text(json.dumps(matches,ensure_ascii=False,indent=2),encoding='utf-8')
