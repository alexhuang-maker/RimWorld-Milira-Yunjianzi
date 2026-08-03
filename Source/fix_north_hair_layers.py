from pathlib import Path

from PIL import Image, ImageChops, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
PAWN = ROOT / "Content" / "Textures" / "Yunjianzi" / "Pawn"
FRONT_PATH = PAWN / "HairFront" / "YunjianziHairFront_north.png"
BACK_PATH = PAWN / "HairBack" / "YunjianziHairBack_north.png"
HEAD_PATH = PAWN / "Head" / "YunjianziHead_north.png"
PREVIEW_PATH = ROOT / "ArtSource" / "SpriteDraft" / "YunjianziHair_north_corrected_preview.png"


def extract_short_lock(source: Image.Image, polygon: list[tuple[int, int]], height_scale: float) -> tuple[Image.Image, tuple[int, int]]:
    selection = Image.new("L", source.size, 0)
    ImageDraw.Draw(selection).polygon(polygon, fill=255)
    alpha = ImageChops.multiply(source.getchannel("A"), selection)
    bounds = alpha.getbbox()
    if bounds is None:
        raise RuntimeError("The selected side lock contains no visible pixels")

    lock = source.copy()
    lock.putalpha(alpha)
    lock = lock.crop(bounds)
    resized = lock.resize(
        (lock.width, max(1, round(lock.height * height_scale))),
        Image.Resampling.LANCZOS,
    )
    return resized, (bounds[0], bounds[1])


front = Image.open(FRONT_PATH).convert("RGBA")
corrected = Image.new("RGBA", front.size, (0, 0, 0, 0))
back = Image.open(BACK_PATH).convert("RGBA")

# The vanilla/Milira head texture still needs a smooth rear scalp cover.  Reuse
# the upper part of the real tied-hair art instead of the pointed front fringe
# that was previously (and incorrectly) shown from behind.
cap_mask = Image.new("L", back.size, 0)
ImageDraw.Draw(cap_mask).polygon(
    [(0, 0), (511, 0), (511, 308), (344, 334), (168, 334), (0, 308)],
    fill=255,
)
cap = back.copy()
cap.putalpha(ImageChops.multiply(back.getchannel("A"), cap_mask))
corrected.alpha_composite(cap)

# On the rear-facing rotation the front-hair node must only supply the two
# temple/ear-root locks.  The tied central mass belongs entirely to HairBack.
left, left_anchor = extract_short_lock(
    front,
    [
        (167, 276), (192, 281), (201, 310), (199, 350),
        (202, 397), (201, 438), (195, 466), (187, 478),
        (178, 464), (170, 430), (167, 377), (167, 320),
    ],
    0.72,
)
right, right_anchor = extract_short_lock(
    front,
    [
        (345, 276), (320, 281), (311, 310), (313, 350),
        (310, 397), (311, 438), (317, 466), (325, 478),
        (334, 464), (342, 430), (345, 377), (345, 320),
    ],
    0.72,
)

corrected.alpha_composite(left, left_anchor)
corrected.alpha_composite(right, right_anchor)
corrected.save(FRONT_PATH, optimize=True)

# Keep an immediately inspectable layer-order preview: rear hair, head/ears,
# then the two corrected front locks.
preview = Image.new("RGBA", front.size, (42, 104, 78, 255))
preview.alpha_composite(back)
preview.alpha_composite(Image.open(HEAD_PATH).convert("RGBA"))
preview.alpha_composite(corrected)
PREVIEW_PATH.parent.mkdir(parents=True, exist_ok=True)
preview.save(PREVIEW_PATH, optimize=True)

print(FRONT_PATH)
print(PREVIEW_PATH)
