---
name: Kindred Paws
colors:
  surface: '#f9f9ff'
  surface-dim: '#d7d9e5'
  surface-bright: '#f9f9ff'
  surface-container-lowest: '#ffffff'
  surface-container-low: '#f1f3fe'
  surface-container: '#ebedf9'
  surface-container-high: '#e6e8f3'
  surface-container-highest: '#e0e2ed'
  on-surface: '#181c23'
  on-surface-variant: '#414754'
  inverse-surface: '#2d3039'
  inverse-on-surface: '#eef0fb'
  outline: '#717786'
  outline-variant: '#c1c6d7'
  surface-tint: '#005bc0'
  primary: '#0059bb'
  on-primary: '#ffffff'
  primary-container: '#0070ea'
  on-primary-container: '#fefcff'
  inverse-primary: '#adc7ff'
  secondary: '#0c6780'
  on-secondary: '#ffffff'
  secondary-container: '#9ae1ff'
  on-secondary-container: '#09657f'
  tertiary: '#9e3d00'
  on-tertiary: '#ffffff'
  tertiary-container: '#c64f00'
  on-tertiary-container: '#fffbff'
  error: '#ba1a1a'
  on-error: '#ffffff'
  error-container: '#ffdad6'
  on-error-container: '#93000a'
  primary-fixed: '#d8e2ff'
  primary-fixed-dim: '#adc7ff'
  on-primary-fixed: '#001a41'
  on-primary-fixed-variant: '#004493'
  secondary-fixed: '#baeaff'
  secondary-fixed-dim: '#89d0ed'
  on-secondary-fixed: '#001f29'
  on-secondary-fixed-variant: '#004d62'
  tertiary-fixed: '#ffdbcc'
  tertiary-fixed-dim: '#ffb695'
  on-tertiary-fixed: '#351000'
  on-tertiary-fixed-variant: '#7c2e00'
  background: '#f9f9ff'
  on-background: '#181c23'
  surface-variant: '#e0e2ed'
  electric-blue: '#2E5BFF'
  sky-mist: '#E0F2F7'
  status-available: '#28A745'
  status-process: '#FFC107'
  status-adopted: '#007BFF'
  status-unavailable: '#DC3545'
  glass-fill: rgba(255, 255, 255, 0.7)
  glass-stroke: rgba(255, 255, 255, 0.3)
typography:
  headline-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 32px
    fontWeight: '800'
    lineHeight: 40px
    letterSpacing: -0.02em
  headline-lg-mobile:
    fontFamily: Plus Jakarta Sans
    fontSize: 26px
    fontWeight: '800'
    lineHeight: 32px
  headline-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 24px
    fontWeight: '700'
    lineHeight: 30px
  body-lg:
    fontFamily: Plus Jakarta Sans
    fontSize: 18px
    fontWeight: '500'
    lineHeight: 28px
  body-md:
    fontFamily: Plus Jakarta Sans
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  label-bold:
    fontFamily: Plus Jakarta Sans
    fontSize: 14px
    fontWeight: '700'
    lineHeight: 20px
  label-sm:
    fontFamily: Plus Jakarta Sans
    fontSize: 12px
    fontWeight: '600'
    lineHeight: 16px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  base: 8px
  container-padding-mobile: 16px
  container-padding-desktop: 32px
  gutter: 16px
  glass-margin: 12px
---

## Brand & Style
The design system is built on an **Emotional, Joyful, and Professional** foundation. It serves a dual purpose: a vibrant social network for pet lovers and a reliable management tool for shelters. 

The chosen style is **Glassmorphism**. This aesthetic uses frosted glass effects and multi-layered translucency to create a sense of lightness and air. It evokes the feeling of a modern, high-end social app while maintaining the warmth necessary for a pet-centric platform. By layering semi-transparent "vessels" over soft, colorful backgrounds, the UI feels deep and immersive without being cluttered.

### Design Principles
- **Emotional Connection:** Use large, high-quality imagery of animals as the primary focus, framed by soft glass containers.
- **Joyful Vitality:** Vibrant blues and energetic gradients keep the mood optimistic.
- **Professional Reliability:** Despite the playful tone, the interface maintains a strict grid and clear hierarchy to ensure shelters can manage data efficiently.

## Colors
The palette is centered around **Joyful Blues**. The primary brand color is an energetic Electric Blue, supported by soft Sky Mist for background washes. 

To maintain the glassmorphic effect:
- **Backgrounds:** Use subtle radial gradients (e.g., from `#E0F2F7` to `#FFFFFF`) to provide "depth" behind glass layers.
- **Glass Surfaces:** Use `glass-fill` with a `backdrop-filter: blur(12px)`.
- **Status Indicators:** Use the high-saturation named colors for adoption status badges. These should be crisp and solid to ensure they pop against the translucent glass backgrounds.
- **Neutral:** White is used extensively for text on dark backgrounds or as a base for glass layers.

## Typography
**Plus Jakarta Sans** is the sole typeface for this design system. Its soft, rounded terminals and contemporary geometric structure perfectly balance "friendly" and "modern."

- **Headlines:** Use heavy weights (700-800) with slight negative letter spacing to create a punchy, editorial feel for animal names and success stories.
- **Body:** Use medium weights for better legibility on translucent surfaces.
- **Scalability:** On mobile, headlines should scale down to prevent excessive line-breaking, especially for longer shelter names.

## Layout & Spacing
The layout follows a **Fluid Grid** model, optimized for a mobile-first social experience.

- **Mobile (Default):** A 4-column grid with 16px margins. Content cards (Feed items) span the full width of the container.
- **Tablet/Desktop:** A 12-column grid. The "Instagram-style" feed remains centered in a max-width container (approx. 600px) to maintain focus, while administrative dashboards utilize the full width for data density.
- **Spacing Rhythm:** Based on an 8px scale. Glass containers should have consistent internal padding (16px or 24px) to ensure content doesn't touch the blurred edges.

## Elevation & Depth
Depth is created through **Glassmorphism and Ambient Shadows** rather than traditional elevation levels.

- **Surface 1 (Base):** Soft sky blue gradients.
- **Surface 2 (Floating Cards):** Frosted glass (`rgba(255, 255, 255, 0.7)`) with a 12px blur and a 1px white border (`glass-stroke`).
- **Shadows:** Use extremely diffused, low-opacity shadows with a hint of the primary blue color (`rgba(0, 123, 255, 0.1)`) to make cards feel like they are floating above the background.
- **Interactive States:** When a user presses a button or card, increase the background blur and slightly scale the element (0.98) to mimic a physical "press" into the glass.

## Shapes
The shape language is **Rounded (0.5rem base)** to reinforce the friendly and approachable brand tone. 

- **Cards:** Use `rounded-xl` (1.5rem) for main feed cards and animal profiles to give them a soft, welcoming appearance.
- **Interactive Elements:** Buttons and input fields use `rounded-lg` (1.0rem).
- **Avatars/Status Badges:** Use fully circular (pill-shaped) borders to contrast against the rectangular layout of the feed.

## Components
- **Buttons:** Primary buttons should be semi-transparent vibrant blue with high legibility. Ghost buttons use a `glass-stroke` border and no fill until hovered.
- **Glass Cards:** The "Feed Item" card is the hero component. It features a top-aligned image with a bottom frosted glass section containing the animal's name, age, and a quick-action "Like" heart.
- **Adoption Badges:** Small, high-contrast pills (e.g., "Available" in Green) that sit on top of the animal images.
- **Input Fields:** Semi-transparent white backgrounds with a subtle inner shadow. On focus, the border glows with the `electric-blue`.
- **Navigation Bar:** A fixed bottom bar with a heavy backdrop-filter blur, making the content scroll "behind" the navigation icons.
- **Stories:** Circular frames with a gradient border using the `electric-blue` to `secondary-color` range.