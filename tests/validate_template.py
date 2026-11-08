"""Read-only privacy/layout check. Usage: python validate_template.py ORIGINAL.xlsm"""
from pathlib import Path
from zipfile import ZipFile
import xml.etree.ElementTree as E
import sys

N = '{http://schemas.openxmlformats.org/spreadsheetml/2006/main}'
template = Path(__file__).resolve().parents[1] / 'assets' / 'template.xlsm'
with ZipFile(sys.argv[1]) as z:
    original = {p: z.read(p) for p in z.namelist()}
with ZipFile(template) as z:
    clean = {p: z.read(p) for p in z.namelist()}
assert original.keys() == clean.keys(), 'Unexpected package parts'
strings = [''.join(t.text or '' for t in item.iter(N+'t'))
           for item in E.fromstring(original['xl/sharedStrings.xml'])]

def text(cell, shared):
    if cell.get('t') == 's':
        return shared[int(cell.find(N+'v').text)]
    return ''.join(t.text or '' for t in cell.iter(N+'t')) or cell.findtext(N+'v', '')

source_sheet = E.fromstring(original['xl/worksheets/sheet1.xml'])
target_sheet = E.fromstring(clean['xl/worksheets/sheet1.xml'])
target_cells = {c.get('r'): c for c in target_sheet.iter(N+'c')}
identifiers = set()
for row in source_sheet.find(N+'sheetData'):
    for cell in row:
        value = text(cell, strings)
        if int(row.get('r')) < 5:
            assert text(target_cells[cell.get('r')], []) == value, 'Header/hint changed'
        elif cell.get('r', '')[0] in ('A', 'B') and len(value) > 4:
            identifiers.add(value)
for row in target_sheet.find(N+'sheetData'):
    if int(row.get('r')) >= 5:
        assert all(not text(cell, []) for cell in row), 'Customer cell remains'
assert len(E.fromstring(clean['xl/sharedStrings.xml'])) == 0, 'Unused shared strings remain'
for part in original:
    if part not in ('xl/sharedStrings.xml', 'xl/worksheets/sheet1.xml', 'docProps/core.xml'):
        assert original[part] == clean[part], 'Changed layout or binary part'
for identifier in identifiers:
    assert not any(identifier.encode('utf-8') in data or identifier.encode('utf-16le') in data
                   for data in clean.values()), 'Customer identifier remains'
assert original['xl/vbaProject.bin'] == clean['xl/vbaProject.bin'], 'VBA changed'
print('PASS: sanitized template; original headings/hints, styles and VBA preserved; no customer identifiers found.')
