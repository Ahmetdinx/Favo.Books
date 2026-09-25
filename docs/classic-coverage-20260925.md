# Classic coverage — 2026-09-25

Local catalog expanded by seven distinct novels with covered Turkish editions:

- Gurur ve Önyargı — Jane Austen
- Madame Bovary — Gustave Flaubert
- Frankenstein ya da Modern Prometheus — Mary Shelley
- Mai ve Siyah — Halit Ziya Uşaklıgil
- Aşk-ı Memnu — Halit Ziya Uşaklıgil
- Kırmızı ve Siyah — Stendhal
- Sefiller – 2 Cilt — Victor Hugo (one novel, not two works)

Publisher URLs, ISBNs and original source hashes are in
`data/curation/classics-20260925/manifest.json`. Covers were checked for image content
and dimensions; this is not a claim of manual visual inspection of every image.
The source's explicit Roman labels were checked. Madame Bovary and Sefiller have
source-bound editorial decisions because unrelated biography text triggered the
conservative literary-form exclusions.

Preview: 7 new, 0 duplicates. Apply: total records 10,858 → 10,865; eligible novels
1,005 → 1,012. Replay reports 0 new with unchanged counts. No existing library or
rating records were changed. Application is local, not a production deployment.

Read-only title/edition audit also identified covered but unreviewed existing
records such as Suç ve Ceza, Monte Cristo Kontu, Don Kişot and Kürk Mantolu Madonna.
These were not republished; the follow-up work-ID audit was declined. Other gaps
include Jane Eyre, Uğultulu Tepeler, Çalıkuşu, İnce Memed and Saatleri Ayarlama
Enstitüsü; each still needs edition/source review. This is an editorial priority
sample, not an exhaustive classics list or measured popularity ranking.

Collector fix: current publisher JSON-LD includes a commented publisher property
and malformed surrounding graph data. Recover only a complete independently
parseable Product object after removing that exact standalone comment pattern;
retain original source hashes and all downstream ISBN/form/cover validation.
