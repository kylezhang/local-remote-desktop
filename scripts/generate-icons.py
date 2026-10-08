"""Regenerate platform icons from assets/brand/icon.svg (requires Pillow).

Small renderer for the checked-in rectangle, polygon and orthogonal-path design.
SVG geometry is the source of truth; no external service or downloaded artwork.
"""
from pathlib import Path
import re
import xml.etree.ElementTree as ET
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parents[1]
source = root / 'assets/brand/icon.svg'
svg = ET.parse(source).getroot()
scale = 2
image = Image.new('RGBA', (1024*scale, 1024*scale))
draw = ImageDraw.Draw(image)

def render(element, inherited):
    style = {**inherited, **element.attrib}
    kind = element.tag.split('}')[-1]
    width = int(float(style.get('stroke-width', 0))*scale)
    stroke = style.get('stroke')
    if kind == 'rect':
        x,y,w,h = [float(style[k])*scale for k in ('x','y','width','height')]
        outset=width/2 if stroke else 0
        draw.rounded_rectangle((x-outset,y-outset,x+w+outset,y+h+outset), radius=float(style.get('rx',0))*scale+outset,
                              fill=None if style.get('fill')=='none' else style.get('fill'),
                              outline=stroke, width=width)
    elif kind == 'polygon':
        numbers=[float(n)*scale for n in re.findall(r'-?\d+(?:\.\d+)?',style['points'])]
        draw.polygon(list(zip(numbers[::2],numbers[1::2])),fill=style.get('fill'),outline=stroke,width=width)
    elif kind == 'path':
        tokens = re.findall(r'[MmHhVv]|-?\d+(?:\.\d+)?', style['d'])
        paths, points, current = [], [], (0.0,0.0)
        index = 0
        while index < len(tokens):
            command = tokens[index]; index += 1
            if command in 'Mm':
                x,y = float(tokens[index]),float(tokens[index+1]); index+=2
                if command=='m': x+=current[0]; y+=current[1]
                if points: paths.append(points)
                points=[]; current=(x,y)
            elif command in 'Hh':
                value=float(tokens[index]); index+=1
                current=(value if command=='H' else current[0]+value,current[1])
            elif command in 'Vv':
                value=float(tokens[index]); index+=1
                current=(current[0],value if command=='V' else current[1]+value)
            else:
                raise ValueError(f'Unsupported SVG command: {command}')
            points.append(current)
            # SVG implicit coordinate pairs after moveto are line segments.
            while index<len(tokens) and re.match(r'-?\d',tokens[index]):
                x,y=float(tokens[index]),float(tokens[index+1]); index+=2
                if command=='m': x+=current[0]; y+=current[1]
                current=(x,y); points.append(current)
        if points: paths.append(points)
        for path in paths:
            coords=[(x*scale,y*scale) for x,y in path]
            draw.line(coords,fill=stroke,width=width,joint='curve')
            for x,y in coords:
                radius=width/2
                draw.ellipse((x-radius,y-radius,x+radius,y+radius),fill=stroke)
    for child in element: render(child,style)

render(svg,{})
image=image.resize((1024,1024),Image.Resampling.LANCZOS)
image.save(root/'assets/brand/icon.png')
image.save(root/'assets/brand/LocalRemote.ico',sizes=[(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)])
image.save(root/'assets/brand/LocalRemote.icns')
print('Generated PNG, multi-size ICO and ICNS from icon.svg')
