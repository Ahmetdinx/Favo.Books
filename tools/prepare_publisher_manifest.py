"""Merge source facts without approving records or touching the database.
Run only after the collectors have completed, then verify this merged input.
"""
import json,sys
from pathlib import Path
root=Path(sys.argv[1])
base=json.loads((root/'publisher-candidates.json').read_text(encoding='utf-8'))
extra=json.loads((root/'ithaki-candidates.json').read_text(encoding='utf-8')) if (root/'ithaki-candidates.json').exists() else []
genres=json.loads((root/'publisher-genres.json').read_text(encoding='utf-8')) if (root/'publisher-genres.json').exists() else {}
entries={}
for item in base+extra:
    url=item['SourceUrl']
    if url in entries and item!=entries[url]:raise ValueError('Conflicting publisher facts: '+url)
    entries[url]=dict(item)
for url,item in entries.items():
    evidence=genres.get(url)
    if evidence:
        item['CategorySlugs']=sorted(set(item['CategorySlugs']+evidence['CategorySlugs']))
        item['GenreSourceUrls']=evidence['GenreSourceUrls']
        if 'klasikler' in item['CategorySlugs']:item['Priority']=min(item['Priority'],20)
output=root/'merged-publisher-candidates.json'
output.write_text(json.dumps(list(entries.values()),ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'Candidates':len(entries),'WithGenres':sum(bool(e['CategorySlugs']) for e in entries.values()),'Output':str(output)}))
