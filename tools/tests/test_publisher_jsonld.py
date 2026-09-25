import ast
import json
import re
import unittest
from pathlib import Path

# Load only the pure extractor; importing the collector would run its CLI/network loop.
tree = ast.parse(Path('tools/collect_publisher_novels.py').read_text(encoding='utf-8-sig'))
function = next(n for n in tree.body if isinstance(n, ast.FunctionDef) and n.name == 'jsonld')
namespace = {'json': json, 're': re}
exec(compile(ast.Module(body=[function], type_ignores=[]), '<extractor>', 'exec'), namespace)

class Script:
    def __init__(self, text): self.string = text
class Soup:
    def __init__(self, text): self.text = text
    def select(self, selector): return [Script(self.text)]

class ProductExtractionTests(unittest.TestCase):
    def test_valid_graph_unchanged(self):
        product = {'@type': 'Product', 'name': 'Roman'}
        self.assertEqual(list(namespace['jsonld'](Soup(json.dumps({'@graph': [product]})))), [product])
    def test_comment_and_broken_graph_recover_complete_product(self):
        raw = '''{"@graph": [{"@type": "Product", "name": "Roman",
        /*"publisher": "Publisher",*/
        "sku": "9789754587029"}, "bad": 1]}'''
        result = list(namespace['jsonld'](Soup(raw)))
        self.assertEqual(result, [{'@type': 'Product', 'name': 'Roman', 'sku': '9789754587029'}])
    def test_incomplete_product_is_rejected(self):
        self.assertEqual(list(namespace['jsonld'](Soup('{"@type":"Product","name":'))), [])
    def test_comment_like_title_is_preserved(self):
        value = {'@type': 'Product', 'name': '/*publisher*/'}
        self.assertEqual(list(namespace['jsonld'](Soup(json.dumps(value)))), [value])

if __name__ == '__main__': unittest.main()
