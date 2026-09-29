"""Write a per-request audit only after actual import and catalog search checks."""
import json,re,sys
from pathlib import Path
sys.stdout.reconfigure(encoding='utf-8')
batch=sys.argv[1]
if not re.fullmatch('[a-z0-9-]+',batch):raise ValueError('Invalid batch')
data=Path('data/curation')/batch;art=Path('artifacts')/batch
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
manifest={r['Number']:r for r in read(data/'manifest.json')}
applied={r['Number']:r for r in read(data/'applied.json')}
checks={r['Number']:r for r in read(data/'applied.json.catalog-check.json')}
before={r['BookId']:r for r in read(art/'catalog.json')}
after={r['BookId']:r for r in read(art/'catalog-after.json')}
config=read(data/'review-config.json');rows=[]
def visible(b):return b['HasCoveredTurkishEdition'] and b['Decision'] in ('Novel','SelectedClassic')
for n,line in enumerate((data/'requested.txt').read_text(encoding='utf-8').splitlines(),1):
    title,author=map(str.strip,line.split('|'));canonical=config['duplicates'].get(str(n),n)
    result=applied[canonical];entry=manifest[canonical];book=after[result['BookId']]
    assert result['Visible'] and checks[canonical]['FoundInCatalogSearch'] and visible(book)
    state='Yeni eklendi' if result['Created'] else ('Zaten görünürdü' if visible(before[result['BookId']]) else 'Görünür yapıldı')
    rows.append(dict(Number=n,RequestedTitle=title,RequestedAuthor=author,CanonicalNumber=canonical,
        CatalogTitle=result['Title'],BookId=result['BookId'],Status=state,Visible=True,SourceUrl=entry['SourceUrl']))
(data/'request-results.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
counts=dict(Requested=len(rows),DistinctWorks=len(applied),Created=sum(r['Created'] for r in applied.values()),
    PreviouslyVisible=sum(not r['Created'] and visible(before[r['BookId']]) for r in applied.values()),
    MadeVisible=sum(not r['Created'] and not visible(before[r['BookId']]) for r in applied.values()),
    CatalogBefore=sum(visible(b) for b in before.values()),CatalogAfter=sum(visible(b) for b in after.values()),
    TotalBefore=len(before),TotalAfter=len(after))
(data/'summary.json').write_text(json.dumps(counts,indent=2),encoding='utf-8')
lines=['# İkinci kitap listesi — sonuçlar','',
    f"{len(rows)} talep satırı / {len(applied)} ayrı eser: tamamı yerel katalogda görünür.",'',
    '| # | İstenen eser | Katalogdaki ad | İşlem |','|---|---|---|---|']
for r in rows:
    duplicate=f"; #{r['CanonicalNumber']} ile aynı eser" if r['Number']!=r['CanonicalNumber'] else ''
    lines.append(f"| {r['Number']} | {r['RequestedTitle']} — {r['RequestedAuthor']} | [{r['CatalogTitle']}]({r['SourceUrl']}) | {r['Status']}{duplicate} |")
Path('docs',batch+'-results.md').write_text('\n'.join(lines)+'\n',encoding='utf-8')
print(json.dumps(counts,ensure_ascii=False))
