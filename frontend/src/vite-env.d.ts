/// <reference types="vite/client" />

declare namespace google {
  namespace maps {
    class Map {
      constructor(mapDiv: Element | null, opts?: any)
      setCenter(latLng: any): void
      panTo(latLng: any): void
      setZoom(zoom: number): void
      addListener(eventName: string, handler: (...args: any[]) => void): any
    }
    class Marker {
      constructor(opts?: any)
      setPosition(latLng: any): void
      getPosition(): any
      setMap(map: Map | null): void
      addListener(eventName: string, handler: (...args: any[]) => void): any
    }
    class InfoWindow {
      constructor(opts?: any)
      open(map: Map, anchor?: any): void
      close(): void
    }
    enum Animation {
      DROP = 1,
      BOUNCE = 2,
    }
    interface MapMouseEvent {
      latLng: {
        lat: () => number
        lng: () => number
      } | null
    }
  }
}

interface Window {
  google?: {
    accounts?: {
      id: {
        initialize: (config: {
          client_id: string
          callback: (response: { credential: string }) => void
        }) => void
        renderButton: (parent: HTMLElement, options: Record<string, unknown>) => void
      }
    }
    maps?: typeof google.maps
  }
}

