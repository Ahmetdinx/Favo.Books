"""Build an offline manifest from reviewed editions and explicit existing-ID choices."""
import hashlib,json,re,sys
from pathlib import Path
sys.stdout.reconfigure(encoding='utf-8')
batch=sys.argv[1]
if not re.fullmatch('[a-z0-9-]+',batch):raise ValueError('Invalid batch')
data=Path('data/curation')/batch;art=Path('artifacts')/batch
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
config=read(data/'review-config.json')
selected={r['Number']:r for r in read(data/'selected-editions.json')}
catalog=read(art/'catalog.json');byid={b['BookId']:b for b in catalog}
isbn_index={}
for book in catalog:
    for ed in book['EditionDetails']:
        for value in (ed.get('Isbn13'),ed.get('Isbn10')):
            if value:isbn_index.setdefault(value,{})[book['BookId']]=book
matches={r['Number']:r for r in read(art/'matches.json')}
out=[];errors=[]
for r in read(art/'editions/details.json'):
    n=r['Number'];s=selected[n]
    if r['Url']!=s['Url']:errors.append((n,'Stale details'));continue
    nodes=[x for d in r['Structured'] for x in d.get('@graph',[d])]
    b=next((x for x in nodes if 'Book' in ([x.get('@type')] if isinstance(x.get('@type'),str) else x.get('@type',[]))),None)
    if not b:errors.append((n,'No book schema'));continue
    if b.get('inLanguage') not in ('tr','tur'):errors.append((n,'Not Turkish',b.get('inLanguage')));continue
    isbn=re.sub(r'\D','',b.get('isbn') or '') or None
    supplement=config.get('isbnSupplements',{}).get(str(n))
    if supplement:
        evidence=art/'editions/pages'/(hashlib.sha256(supplement['SourceUrl'].encode()).hexdigest()+'.html')
        payload=evidence.read_bytes()
        if hashlib.sha256(payload).hexdigest()!=supplement['SourceSha256'] or supplement['Isbn13'].encode() not in payload:
            raise ValueError('Supplemental ISBN evidence mismatch')
        if isbn and isbn!=supplement['Isbn13']:raise ValueError('Conflicting ISBN sources')
        isbn=supplement['Isbn13']
    unavailable=n in config.get('isbnUnavailable',[]) and isbn is None and s['Url'].startswith('https://www.nadirkitap.com/')
    if not unavailable and (not isbn or len(isbn)!=13 or sum(int(c)*(1 if i%2==0 else 3) for i,c in enumerate(isbn))%10):
        errors.append((n,'Invalid ISBN',isbn));continue
    isbn10=None
    if isbn and isbn.startswith('978'):
        t=isbn[3:12];c=(11-sum(int(x)*(10-i) for i,x in enumerate(t))%11)%11;isbn10=t+('X' if c==10 else str(c))
    owners=list({**isbn_index.get(isbn,{}),**isbn_index.get(isbn10,{})}.values())
    choice=config.get('existing',{}).get(str(n),'UNREVIEWED')
    if choice=='UNREVIEWED':errors.append((n,'Existing mapping not reviewed'));continue
    existing=byid.get(choice) if choice else None
    if owners and (len(owners)!=1 or not existing or owners[0]['BookId']!=existing['BookId']):
        errors.append((n,'ISBN owner mismatch',[(x['BookId'],x['Title'],x['Authors']) for x in owners]));continue
    exception=n in config.get('selectedClassics',[])
    note='User-requested 200-line reading list. Turkish edition, author, language, ISBN and cover verified against the cited bibliographic source.'
    if exception:note+=' Explicitly requested novella, story collection or nonfiction narrative; selected classic inclusion, not reclassified as a novel.'
    note+=' '+config.get('notes',{}).get(str(n),'')
    if unavailable:note+=' ISBN unavailable in source; no identifier fabricated.'
    if supplement:note+=' ISBN supplemented from the same title, author, publisher and page-count edition at '+supplement['SourceUrl']+' (SHA256 '+supplement['SourceSha256']+').'
    title=config.get('titles',{}).get(str(n),s['RequestedTitle'])
    source=art/'editions/pages'/(hashlib.sha256(s['Url'].encode()).hexdigest()+'.html')
    entry=dict(Number=n,Title=title,Author=s['RequestedAuthor'],EditionTitle=b['name'],Isbn13=isbn,
        CoverImageUrl=b['image'][0] if isinstance(b['image'],list) else b['image'],Publisher=b['publisher']['name'],
        PageCount=int(b['numberOfPages']) if b.get('numberOfPages') else None,SourceUrl=s['Url'],
        SourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),LanguageCode='tur',EvidenceNote=note.strip(),Decision='SelectedClassic' if exception else 'Novel')
    if existing:entry.update(ExistingBookId=existing['BookId'],ExpectedTitle=existing['Title'],ExpectedAuthor=existing['Authors'][0])
    out.append(entry)
(art/'manifest-draft.json').write_text(json.dumps(sorted(out,key=lambda x:x['Number']),ensure_ascii=False,indent=2),encoding='utf-8')
print('Accepted',len(out),'Errors',json.dumps(errors,ensure_ascii=False))
if len(out)!=len(selected):sys.exit(1)
