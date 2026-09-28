"""Read-only candidate matching; matches require editorial review before import."""
import json, re, unicodedata, sys
from pathlib import Path
sys.stdout.reconfigure(encoding='utf-8')

ROOT = Path('data/curation/reading-list-100-20260928')
ART = Path('artifacts/reading-list-100-20260928')
def key(s):
    s = re.sub(r'\([^)]*\)|\[[^]]*\]', '', s)
    s = re.split(r'\s+[–—-]\s+', s)[0]
    return ''.join(c for c in unicodedata.normalize('NFKD', s.lower().replace('ı','i')) if c.isalnum())

aliases = {
 1:['Don Quixote'], 2:['A Tale of Two Cities','İki Şehrin Hikayesi'],
 3:['The Little Prince'],4:['The Lord of the Rings'],5:['The Alchemist'],
 6:["Harry Potter and the Philosopher’s Stone","Harry Potter and the Sorcerer’s Stone"],
 7:['On Küçük Zenci','Ve Sonra Hiçbiri','On Kişiydiler','And Then There Were None'],
 10:["Alice’s Adventures in Wonderland"],11:['Dişi','She'],
 12:['Aslan Cadı ve Dolap','The Lion the Witch and the Wardrobe'],
 14:['Gönülçelen','The Catcher in the Rye'],16:['One Hundred Years of Solitude'],
 20:['Yeşilin Kızı Anne','Yeşil Çatıların Anne’i','Anne of Green Gables'],
 24:['Watership Down'],25:['Charlotte’s Web'],27:['Martı','Jonathan Livingston Seagull'],
 28:['Bin Dokuz Yüz Seksen Dört','Nineteen Eighty-Four'],29:['Animal Farm'],
 32:['The Great Gatsby'],38:['İhtiyar Adam ve Deniz','The Old Man and the Sea'],
 42:['Crime and Punishment'],43:['The Brothers Karamazov'],45:['Cinler','Demons'],
 48:['War and Peace'],49:['Anna Karenin'],51:['İvan İlyiç’in Ölümü'],
 56:['Usta ve Margarita','The Master and Margarita'],59:['Yevgeniy Onegin','Yevgeni Onyegin'],
 61:['Monte Cristo Kontu','The Count of Monte Cristo'],64:['Sefiller – 2 Cilt','Les Misérables'],
 65:['Notre Dame de Paris','Notre Dame’ın Kamburu'],66:['Madame Bovary'],
 74:['Eugenie Grandet'],88:['Narkissos ve Goldmund'],89:['Das Glasperlenspiel'],
 93:['Sanatçının Bir Genç Adam Olarak Portresi'],95:['Bayan Dalloway'],
 98:['Pride and Prejudice','Aşk ve Gurur']}

catalog=json.loads((ART/'catalog.json').read_text(encoding='utf-8-sig'))
candidates={}
for path in Path('artifacts').glob('*/**/candidates.json'):
    if 'python-deps' in str(path): continue
    try:
        data=json.loads(path.read_text(encoding='utf-8-sig'))
        if isinstance(data,dict): candidates.update(data)
    except (ValueError,PermissionError): pass
result=[]
for i,line in enumerate((ROOT/'requested.txt').read_text(encoding='utf-8').splitlines(),1):
    title,author=map(str.strip,line.split('|'))
    keys={key(t) for t in [title]+aliases.get(i,[])}
    matches=[b for b in catalog if any(key(t) in keys for t in [b['Title']]+b['Editions'])]
    sources=[dict(v,SourceUrl=u) for u,v in candidates.items() if key(v.get('Title','')) in keys]
    result.append(dict(Number=i,Title=title,Author=author,Matches=matches,Sources=sources))
(ART/'matches.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
for r in result:
    print(f"{r['Number']:3} {r['Title']}: " + '; '.join(f"{b['Title']} ({','.join(b['Authors'])}) [{b['Decision']}]" for b in r['Matches']) + f" | sources {len(r['Sources'])}")
