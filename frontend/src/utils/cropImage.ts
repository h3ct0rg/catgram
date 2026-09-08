import { Area } from 'react-easy-crop'

function loadImage(src: string): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const image = new Image()
    image.onload = () => resolve(image)
    image.onerror = () => reject(new Error('No se pudo cargar la imagen.'))
    image.src = src
  })
}

// Drawing through an <img> element bakes in the browser's own EXIF-orientation handling (the
// standard image-orientation: from-image behavior) — the resulting canvas pixels are always
// right-side up regardless of the source file's EXIF Orientation tag, and canvas output carries
// no EXIF of its own, so there is nothing left to misinterpret downstream.
export async function getCroppedImageFile(
  file: File,
  croppedAreaPixels: Area,
  fileName: string,
): Promise<File> {
  const objectUrl = URL.createObjectURL(file)
  try {
    const image = await loadImage(objectUrl)
    const canvas = document.createElement('canvas')
    canvas.width = Math.round(croppedAreaPixels.width)
    canvas.height = Math.round(croppedAreaPixels.height)
    const ctx = canvas.getContext('2d')
    if (!ctx) throw new Error('No se pudo procesar la imagen.')
    ctx.drawImage(
      image,
      croppedAreaPixels.x,
      croppedAreaPixels.y,
      croppedAreaPixels.width,
      croppedAreaPixels.height,
      0,
      0,
      canvas.width,
      canvas.height,
    )
    const blob = await new Promise<Blob | null>((resolve) =>
      canvas.toBlob(resolve, 'image/jpeg', 0.92),
    )
    if (!blob) throw new Error('No se pudo procesar la imagen.')
    return new File([blob], fileName, { type: 'image/jpeg' })
  } finally {
    URL.revokeObjectURL(objectUrl)
  }
}
