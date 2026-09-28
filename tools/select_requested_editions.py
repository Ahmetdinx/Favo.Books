"""Produce review candidates; this selection never authorizes database writes."""
import json,re,unicodedata,sys
from pathlib import Path
sys.stdout.reconfigure(encoding='utf-8')
def key(s):return ''.join(c for c in unicodedata.normalize('NFKD',s.lower().replace('ı','i')) if c.isalnum())
root=Path('data/curation/reading-list-100-20260928')
rows=json.loads(Path('artifacts/reading-list-100-20260928/editions/search.json').read_text(encoding='utf-8'))
preferred=['TÜRKİYE İŞ BANKASI','YAPI KREDİ','CAN YAYINLARI','İLETİŞİM','METİS','İTHAKİ','ALTIN KİTAPLAR','ALFA','TİMAŞ','DOĞAN','KORİDOR','DORLİON']
selected=[]
for row in rows:
    surname=key(row['Author'].split()[-1])
    choices=[c for c in row['Candidates'] if surname in key(c['Author']) and '/kitap/' in c['Url']
        and not re.search(r'kısalt|İngilizce|English|Fransızca|Çizgi|Sticker|özeti|inceleme|defter|hikayeleri',c['Title'],re.I)
        and not (row['Number'] in {27,55,60} and key(row['Title'].split()[0]) not in key(c['Title']))
        and not (row['Number']==55 and ('&' in c['Title'] or 'Petersburg' in c['Title']))
        and not (row['Number']==60 and 'Bütün' in c['Title'])]
    choices.sort(key=lambda c:next((i for i,p in enumerate(preferred) if p in c['Publisher']),99))
    if not choices:print('MISSING',row['Number'],row['Title'],[(c['Title'],c['Author']) for c in row['Candidates'][:4]]);continue
    c=choices[0];selected.append(dict(Number=row['Number'],RequestedTitle=row['Title'],RequestedAuthor=row['Author'],**c))
    print(row['Number'],c['Title'],'|',c['Author'],'|',c['Publisher'])
(Path('artifacts/reading-list-100-20260928')/'selected-editions-candidates.json').write_text(json.dumps(selected,ensure_ascii=False,indent=2),encoding='utf-8')
print('SELECTED',len(selected))
