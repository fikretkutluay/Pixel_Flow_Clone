"""Copy the generated textures into the Unity project and author their .meta
import settings, so no manual Inspector work is needed.

GUIDs are derived from the asset path, so re-running this never orphans an
existing reference.
"""
import glob
import hashlib
import os
import pathlib
import shutil

SRC = "out"
DST = str(pathlib.Path(__file__).resolve().parents[2] / "Assets/_Game/Art/Sprites/UI")

# spriteBorder is (x=left, y=bottom, z=right, w=top)
BORDERS = [
    ("pixelflow_ui_panel_header", (34, 0, 34, 34)),
    ("pixelflow_ui_panel", (34, 34, 34, 34)),
    ("pixelflow_ui_buttonsq_", (84, 0, 84, 0)),
    ("pixelflow_ui_button_", (59, 0, 59, 0)),
    ("pixelflow_ui_iconframe", (62, 62, 62, 62)),
    ("pixelflow_ui_ribbon", (100, 0, 100, 0)),
    ("pixelflow_ui_pill", (60, 0, 60, 0)),
    ("pixelflow_ui_circle", (0, 0, 0, 0)),
    ("pixelflow_icon_", (0, 0, 0, 0)),
]

PLATFORMS = ["DefaultTexturePlatform", "iOS", "Android", "Standalone",
             "WebGL", "WindowsStoreApps"]


def guid_for(path):
    return hashlib.md5(("pixelflow::" + path).encode()).hexdigest()


def border_for(name):
    for prefix, b in BORDERS:
        if name.startswith(prefix):
            return b
    raise SystemExit(f"no border rule for {name}")


def platform_block(target):
    # textureCompression 0 = None, keeping UI edges crisp (GDD 4.3)
    return f"""  - serializedVersion: 4
    buildTarget: {target}
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
"""


def meta_for(asset_path, border):
    x, y, z, w = border
    return f"""fileFormatVersion: 2
guid: {guid_for(asset_path)}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
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
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: {x}, y: {y}, z: {z}, w: {w}}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
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
{''.join(platform_block(p) for p in PLATFORMS)}  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def folder_meta(path):
    return (f"fileFormatVersion: 2\nguid: {guid_for(path)}\n"
            "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
            "  userData: \n  assetBundleName: \n  assetBundleVariant: \n")


def main():
    os.makedirs(DST, exist_ok=True)
    rel_dir = "Assets/_Game/Art/Sprites/UI"
    if not os.path.exists(DST + ".meta"):
        open(DST + ".meta", "w", newline="\n").write(folder_meta(rel_dir))

    files = sorted(glob.glob(f"{SRC}/pixelflow_*.png"))
    for f in files:
        name = os.path.basename(f)
        rel = f"{rel_dir}/{name}"
        shutil.copyfile(f, f"{DST}/{name}")
        open(f"{DST}/{name}.meta", "w", newline="\n").write(
            meta_for(rel, border_for(name[:-4])))
    print(f"imported {len(files)} sprites -> {rel_dir}")


if __name__ == "__main__":
    main()
