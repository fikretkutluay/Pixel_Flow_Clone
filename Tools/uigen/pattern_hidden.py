"""Generate the seamless "?" pattern that marks a hidden shooter.

The reference game identifies its mystery shooter by a *pattern*, not a colour:
small question marks at random rotations scattered over the body, in a lighter
tint of it. Colour alone would stop working the moment the palette grows dark
greens or blacks, which is exactly what later levels add.

The texture is white-on-transparent. The shader tints it, so one texture serves
every body colour.

Run:  python pattern_hidden.py       (writes the PNG and its Unity .meta)
"""
import hashlib
import math
import os
import pathlib
import random

from PIL import Image, ImageDraw, ImageFont

ROOT = pathlib.Path(__file__).resolve().parents[2]
FONT = ROOT / "Assets/_Game/Art/Text/Baloo2-ExtraBold.ttf"
DST_DIR = ROOT / "Assets/_Game/Art/Textures"
ASSET_DIR = "Assets/_Game/Art/Textures"
NAME = "pixelflow_pattern_question.png"

SIZE = 512
GRID = 4            # 4x4 jittered cells -> 16 glyphs per tile
JITTER = 0.30       # cell fraction a glyph centre may wander
# Sized so the pattern covers ~26% of the body, matching the reference (26.1%
# measured off the mystery shooter, background masked out).
GLYPH_FRAC = 0.22   # glyph height as a fraction of the tile
SCALE_VAR = 0.18
SEED = 20260730     # fixed: re-running must not reshuffle the pattern


def guid_for(path):
    # Same derivation as import_to_unity.py, so re-running never orphans a ref.
    return hashlib.md5(("pixelflow::" + path).encode()).hexdigest()


def glyph_image(font, px):
    """A '?' rendered tightly cropped, white on transparent."""
    probe = Image.new("L", (px * 3, px * 3), 0)
    ImageDraw.Draw(probe).text((px, px), "?", font=font, fill=255, anchor="lt")
    box = probe.getbbox()
    if box is None:
        raise SystemExit("font produced no '?' glyph")
    mask = probe.crop(box)

    out = Image.new("RGBA", mask.size, (255, 255, 255, 0))
    out.putalpha(mask)
    out.paste((255, 255, 255), (0, 0), mask)
    return out


def build():
    rng = random.Random(SEED)
    canvas = Image.new("RGBA", (SIZE, SIZE), (255, 255, 255, 0))

    target_h = SIZE * GLYPH_FRAC
    # Rasterise the glyph large, then downscale per placement — keeps the edges
    # smooth at every random rotation instead of re-hinting the font each time.
    font = ImageFont.truetype(str(FONT), int(target_h * 3))
    base = glyph_image(font, int(target_h * 3))

    cell = SIZE / GRID
    placed = 0
    for gy in range(GRID):
        for gx in range(GRID):
            cx = (gx + 0.5 + rng.uniform(-JITTER, JITTER)) * cell
            cy = (gy + 0.5 + rng.uniform(-JITTER, JITTER)) * cell

            scale = 1.0 + rng.uniform(-SCALE_VAR, SCALE_VAR)
            h = target_h * scale
            w = h * base.width / base.height
            g = base.resize((max(1, round(w)), max(1, round(h))), Image.LANCZOS)
            g = g.rotate(rng.uniform(0, 360), resample=Image.BICUBIC, expand=True)

            # Paste the glyph and every wrapped copy, so glyphs crossing an edge
            # continue on the far side and the tile stays seamless.
            ox, oy = cx - g.width / 2, cy - g.height / 2
            for dx in (-SIZE, 0, SIZE):
                for dy in (-SIZE, 0, SIZE):
                    canvas.paste(g, (round(ox + dx), round(oy + dy)), g)
            placed += 1

    return canvas, placed


TEXTURE_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 1
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 1
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 512
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 4
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 1
  lightmap: 0
  compressionQuality: 50
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 512
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Android
    maxTextureSize: 512
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""

FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def main():
    img, placed = build()
    DST_DIR.mkdir(parents=True, exist_ok=True)

    folder_meta = str(DST_DIR) + ".meta"
    if not os.path.exists(folder_meta):
        open(folder_meta, "w", newline="\n").write(
            FOLDER_META.format(guid=guid_for(ASSET_DIR)))

    out = DST_DIR / NAME
    img.save(out)

    rel = f"{ASSET_DIR}/{NAME}"
    meta = str(out) + ".meta"
    if not os.path.exists(meta):
        open(meta, "w", newline="\n").write(
            TEXTURE_META.format(guid=guid_for(rel)))

    alpha = img.getchannel("A")
    coverage = sum(alpha.getdata()) / (255 * SIZE * SIZE)
    print(f"{rel}  {SIZE}x{SIZE}  {placed} glyphs  coverage {coverage:.1%}")


if __name__ == "__main__":
    main()
