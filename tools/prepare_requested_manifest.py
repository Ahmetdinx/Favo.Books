"""Extract bibliographic facts from individually reviewed edition selections."""
import hashlib,json,re,sys
from pathlib import Path
sys.stdout.reconfigure(encoding='utf-8')
root=Path('data/curation/reading-list-100-20260928');art=Path('artifacts/reading-list-100-20260928')
details=json.loads((art/'editions/details.json').read_text(encoding='utf-8'))
selected={r['Number']:r for r in json.loads((root/'selected-editions.json').read_text(encoding='utf-8-sig'))}
matches={r['Number']:r for r in json.loads((art/'matches.json').read_text(encoding='utf-8'))}
catalog=json.loads((art/'catalog.json').read_text(encoding='utf-8-sig'))
out=[];errors=[]
for row in details:
    n=row['Number']
    if row['Url']!=selected[n]['Url']:continue
    nodes=[x for d in row['Structured'] for x in d.get('@graph',[d])]
    book=next((x for x in nodes if x.get('@type')=='Book' or (isinstance(x.get('@type'),list) and 'Book' in x['@type'])),None)
    if not book:errors.append((n,'No structured book'));continue
    if book.get('inLanguage') not in ('tr','tur'):errors.append((n,'Not Turkish',book.get('inLanguage')));continue
    isbn=re.sub(r'\D','',book.get('isbn','') or '') or None
    if n!=23 and (not isbn or len(isbn)!=13 or sum(int(c)*(1 if i%2==0 else 3) for i,c in enumerate(isbn))%10):errors.append((n,'Invalid ISBN',isbn));continue
    author=book.get('author',{})
    authors=author if isinstance(author,list) else [author]
    print(n,book['name'],'|',','.join(a.get('name','') for a in authors),'|',isbn,'|',book.get('numberOfPages'),flush=True)
    existing=matches[n]['Matches'] if n not in {42,76,77,96} else []
    if n==25:existing=[b for b in catalog if b['Title']=='Orumcek Agi' and 'E. B. White' in b['Authors']]
    if n==58:existing=[b for b in existing if 'Максим Горький' in b['Authors']]
    if n==48:existing=[b for b in existing if b['Title']=='Savas ve Baris']
    if len(existing)>1:errors.append((n,'Ambiguous existing mapping'));continue
    title=matches[n]['Title']
    title={7:'On Kişiydiler',11:'Kadın',19:'Kara İnci',25:'Örümcek Ağı',26:'Zencefil Adam'}.get(n,title)
    exception=n in {3,27,55,83,91,94}
    note='User-requested 100-work list. Edition title, author, Turkish language, ISBN and cover checked against the cited bibliographic page.'
    if exception:note+=' Explicit catalog inclusion requested; fairy tale, novella, short story, drama or story collection, not reclassified as a novel.'
    if n==8:note+=' Turkish source edition is volume 1 of the same work; edition title retains the volume number.'
    if n==59:note+=' Verse novel.'
    if n==23:note+=' ISBN not assigned in the cited 1978 Altın Kitaplar edition; no ISBN fabricated.'
    source=art/'editions/pages'/(hashlib.sha256(row['Url'].encode()).hexdigest()+'.html')
    entry=dict(Number=n,Title=title,Author=matches[n]['Author'],EditionTitle=book['name'],Isbn13=isbn,
        CoverImageUrl=book['image'][0] if isinstance(book['image'],list) else book['image'],Publisher=book['publisher']['name'],PageCount=int(book['numberOfPages']) if book.get('numberOfPages') else None,
        SourceUrl=row['Url'],SourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),LanguageCode='tur',
        EvidenceNote=note,Decision='SelectedClassic' if exception else 'Novel')
    if existing:
        b=existing[0];entry.update(ExistingBookId=b['BookId'],ExpectedTitle=b['Title'],ExpectedAuthor=b['Authors'][0])
    out.append(entry)
(art/'manifest-draft.json').write_text(json.dumps(sorted(out,key=lambda x:x['Number']),ensure_ascii=False,indent=2),encoding='utf-8')
print('ACCEPTED',len(out),'ERRORS',errors)
