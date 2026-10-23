extends Control

@onready var artwork_clip: Control = $ArtworkClip
@onready var artwork: TextureRect = $ArtworkClip/Artwork

func _ready():
	artwork_clip.resized.connect(_fit_artwork)
	_fit_artwork()

func _fit_artwork():
	if artwork.texture == null:
		return
	var source_size = artwork.texture.get_size()
	var cover_scale = maxf(artwork_clip.size.x / source_size.x, artwork_clip.size.y / source_size.y)
	artwork.size = source_size * cover_scale
	# Crop the lower robe, not the face; horizontal cropping stays centered.
	artwork.position = Vector2((artwork_clip.size.x - artwork.size.x) * 0.5, 0)
