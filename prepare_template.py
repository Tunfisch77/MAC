"""Development-only: remove customer data before distributing the app."""
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
import xml.etree.ElementTree as E
import sys
import struct
from PIL import Image
from io import BytesIO

root=Path(__file__).parent
source=Path(sys.argv[1])
n='{http://schemas.openxmlformats.org/spreadsheetml/2006/main}'
with ZipFile(source) as z: parts={k:z.read(k) for k in z.namelist()}
strings=[''.join(t.text or '' for t in si.iter(n+'t')) for si in E.fromstring(parts['xl/sharedStrings.xml'])]
sheet=E.fromstring(parts['xl/worksheets/sheet1.xml'])
for row in sheet.find(n+'sheetData'):
    num=int(row.get('r'))
    for c in row.findall(n+'c'):
        if num>=5:
            for child in list(c):c.remove(child)
            c.attrib.pop('t',None)
        elif c.get('t')=='s':
            text=strings[int(c.find(n+'v').text)]
            for child in list(c):c.remove(child)
            c.set('t','inlineStr')
            E.SubElement(E.SubElement(c,n+'is'),n+'t').text=text
parts['xl/worksheets/sheet1.xml']=E.tostring(sheet,encoding='utf-8',xml_declaration=True)
parts['xl/sharedStrings.xml']=E.tostring(E.Element(n+'sst',{'count':'0','uniqueCount':'0'}),encoding='utf-8',xml_declaration=True)
if 'docProps/core.xml' in parts:
    parts['docProps/core.xml']=b'<?xml version="1.0"?><cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties"/>'
with ZipFile(root/'assets/template.xlsm','w',ZIP_DEFLATED) as z:
    for k,v in parts.items():z.writestr(k,v)
logo=parts['xl/media/image.png'];(root/'assets/logo.png').write_bytes(logo)
im=Image.open(BytesIO(logo)).convert('RGBA')
symbol=im.crop((0,0,min(im.height,im.width),im.height))
canvas=Image.new('RGBA',(256,256),(43,81,78,255));symbol.thumbnail((220,220));canvas.alpha_composite(symbol,((256-symbol.width)//2,(256-symbol.height)//2))
# Classic DIB entries with an explicit AND mask also work with older Icon readers.
sizes=[16,32,48,64,128,256]
images=[]
for size in sizes:
    scaled=canvas.resize((size,size),Image.Resampling.LANCZOS)
    pixels=scaled.tobytes('raw','BGRA',0,-1)
    mask=bytes(((size+31)//32)*4*size)
    header=struct.pack('<IIIHHIIIIII',40,size,size*2,1,32,0,len(pixels),0,0,0,0)
    images.append(header+pixels+mask)
offset=6+16*len(images)
entries=[]
for size,data in zip(sizes,images):
    entries.append(struct.pack('<BBBBHHII',size%256,size%256,0,0,1,32,len(data),offset))
    offset+=len(data)
(root/'assets/mac.ico').write_bytes(struct.pack('<HHH',0,1,len(images))+b''.join(entries)+b''.join(images))
# Verify no populated customer cells or original shared strings remain.
assert not any(c.find(n+'v') is not None or c.find(n+'is') is not None for row in sheet.find(n+'sheetData') if int(row.get('r'))>=5 for c in row)
print('Sanitized template, supplied logo and icon prepared. No customer rows distributed.')
