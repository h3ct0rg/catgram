import { useCallback, useEffect, useState } from 'react'
import Cropper, { Area, Point } from 'react-easy-crop'
import { getCroppedImageFile } from '../../utils/cropImage'

export type CropAspectOption = { label: string; value: number }

type Props = {
  file: File
  aspectOptions: CropAspectOption[]
  title?: string
  onCancel: () => void
  onConfirm: (croppedFile: File) => void
}

export function ImageCropModal({
  file,
  aspectOptions,
  title = 'Ajustar imagen',
  onCancel,
  onConfirm,
}: Props) {
  const [imageUrl, setImageUrl] = useState<string | null>(null)
  const [crop, setCrop] = useState<Point>({ x: 0, y: 0 })
  const [zoom, setZoom] = useState(1)
  const [aspect, setAspect] = useState(aspectOptions[0]?.value ?? 1)
  const [croppedAreaPixels, setCroppedAreaPixels] = useState<Area | null>(null)
  const [processing, setProcessing] = useState(false)

  // Create the object URL inside the effect (not via a useState lazy initializer) so that
  // StrictMode's dev-only double mount/cleanup/mount pass always creates a fresh URL on its second
  // setup — creating it outside the effect meant the phantom cleanup revoked the only URL that
  // existed, leaving the <img> pointed at an already-revoked blob (net::ERR_FILE_NOT_FOUND).
  useEffect(() => {
    const url = URL.createObjectURL(file)
    setImageUrl(url)
    return () => URL.revokeObjectURL(url)
  }, [file])

  const handleCropComplete = useCallback((_area: Area, pixels: Area) => {
    setCroppedAreaPixels(pixels)
  }, [])

  async function confirm() {
    if (!croppedAreaPixels) return
    setProcessing(true)
    try {
      const croppedFile = await getCroppedImageFile(file, croppedAreaPixels, file.name)
      onConfirm(croppedFile)
    } finally {
      setProcessing(false)
    }
  }

  return (
    <div className="modal-backdrop" role="dialog" aria-modal="true">
      <div className="crop-modal glass-card">
        <div className="create-story-header">
          <div className="create-story-header-left">
            <span className="material-symbols-outlined create-story-icon">crop</span>
            <div>
              <h2>{title}</h2>
              <p>Arrastra y usa el zoom para ajustar el encuadre</p>
            </div>
          </div>
          <button type="button" className="modal-close-btn" onClick={onCancel} aria-label="Cerrar">
            <span className="material-symbols-outlined">close</span>
          </button>
        </div>

        <div className="crop-modal-canvas-wrap">
          {imageUrl && (
            <Cropper
              image={imageUrl}
              crop={crop}
              zoom={zoom}
              aspect={aspect}
              cropShape="rect"
              showGrid
              onCropChange={setCrop}
              onZoomChange={setZoom}
              onCropComplete={handleCropComplete}
            />
          )}
        </div>

        <div className="crop-modal-controls">
          {aspectOptions.length > 1 && (
            <div className="crop-aspect-options">
              {aspectOptions.map((option) => (
                <button
                  key={option.label}
                  type="button"
                  className={aspect === option.value ? 'active' : ''}
                  onClick={() => setAspect(option.value)}
                >
                  {option.label}
                </button>
              ))}
            </div>
          )}
          <div className="crop-modal-zoom">
            <span className="material-symbols-outlined">zoom_out</span>
            <input
              type="range"
              min={1}
              max={3}
              step={0.05}
              value={zoom}
              onChange={(event) => setZoom(Number(event.target.value))}
            />
            <span className="material-symbols-outlined">zoom_in</span>
          </div>
          <div className="crop-modal-actions">
            <button
              className="secondary-button"
              type="button"
              onClick={onCancel}
              disabled={processing}
            >
              Cancelar
            </button>
            <button
              className="primary-button"
              type="button"
              onClick={confirm}
              disabled={processing || !croppedAreaPixels}
            >
              {processing ? 'Procesando…' : 'Usar foto'}
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
