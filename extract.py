import zipfile, xml.etree.ElementTree as ET

path = '/home/nerdyamin/Documents/repos/mcg/Template of Daily Report.docx'
WORD_NS = 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'

def get_text(elem):
return ''.join(t.text for t in elem.iter(f'{{{WORD_NS}}}t') if t.text)

with zipfile.ZipFile(path) as z:
with z.open('word/document.xml') as f:
root = ET.parse(f).getroot()

body = root.find(f'{{{WORD_NS}}}body')
for child in body:
tag = child.tag.split('}')[-1]
if tag == 'p':
text = get_text(child)
if text.strip():
print(f'PARA: {text}')
elif tag == 'tbl':
print('TABLE:')
for row in child.findall(f'{{{WORD_NS}}}tr'):
cells = [get_text(c).strip() for c in row.findall(f'{{{WORD_NS}}}tc')]
print(' ROW:', ' | '.join(cells))