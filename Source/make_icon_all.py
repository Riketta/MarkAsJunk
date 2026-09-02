"""Regenerates Textures/UI/Commands/MarkAsJunkAll.png (32x32 RGBA icon for the
storage "treat contents as junk" toggle: a trash can inside a zone border).

No external dependencies: hand-rolled PNG chunks with zlib.
Usage:  python3 make_icon_all.py   (from Source/, or adjust the output path)
"""
import os
import struct
import zlib

W = H = 32
OUT = (0, 0, 0, 0)
OUTLINE = (44, 44, 54, 255)
BORDER = (110, 110, 122, 255)
LID = (152, 152, 162, 255)
BODY = (172, 172, 182, 255)
RIDGE = (118, 118, 132, 255)
HANDLE = (132, 132, 142, 255)

px = [[OUT] * W for _ in range(H)]


def rect(x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if 0 <= x < W and 0 <= y < H:
                px[y][x] = c


def outlined(x0, y0, x1, y1, fill):
    rect(x0, y0, x1, y1, OUTLINE)
    rect(x0 + 1, y0 + 1, x1 - 1, y1 - 1, fill)


# zone border (1px, open interior)
rect(2, 2, 29, 2, BORDER)
rect(2, 29, 29, 29, BORDER)
rect(2, 3, 2, 28, BORDER)
rect(29, 3, 29, 28, BORDER)

# smaller trash can inside the border: handle, lid, body, ridges
outlined(14, 6, 17, 8, HANDLE)
outlined(7, 9, 24, 12, LID)
outlined(9, 14, 22, 27, BODY)
rect(12, 17, 13, 24, RIDGE)
rect(15, 17, 16, 24, RIDGE)
rect(18, 17, 19, 24, RIDGE)

raw = b''
for y in range(H):
    raw += b'\x00' + b''.join(bytes(px[y][x]) for x in range(W))


def chunk(tag, data):
    c = struct.pack('>I', len(data)) + tag + data
    return c + struct.pack('>I', zlib.crc32(tag + data) & 0xffffffff)


png = b'\x89PNG\r\n\x1a\n'
png += chunk(b'IHDR', struct.pack('>IIBBBBB', W, H, 8, 6, 0, 0, 0))
png += chunk(b'IDAT', zlib.compress(raw, 9))
png += chunk(b'IEND', b'')

out = os.path.join(os.path.dirname(__file__), '..', 'Textures', 'UI', 'Commands', 'MarkAsJunkAll.png')
with open(out, 'wb') as f:
    f.write(png)
print('written', len(png), 'bytes to', os.path.normpath(out))
