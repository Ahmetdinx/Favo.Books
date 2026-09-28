import json,sys
from pathlib import Path
sys.stdout.reconfigure(encoding='utf-8')
art=Path('artifacts/reading-list-100-20260928')
rows=json.loads((art/'manifest-draft.json').read_text(encoding='utf-8'))
catalog=json.loads((art/'catalog-with-isbns.json').read_text(encoding='utf-8-sig'))
def isbn13(s):
    if not s:return None
    if len(s)==10:
        s='978'+s[:9];s+=str(-sum(int(c)*(1 if i%2==0 else 3) for i,c in enumerate(s))%10)
    return s
for e in rows:
    hits=[b for b in catalog if e['Isbn13'] and any(e['Isbn13'] in [x['Isbn13'],isbn13(x['Isbn10'])] for x in b['EditionDetails'])]
    for b in hits:
        if b['BookId']!=e.get('ExistingBookId'):print('COLLISION',e['Number'],e['Title'],b['BookId'],b['Title'],b['Authors'])
print('Entries',len(rows),'mapped existing',sum('ExistingBookId' in e for e in rows))
