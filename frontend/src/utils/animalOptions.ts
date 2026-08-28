export type SelectOption = { value: string; label: string }

export const SPECIES_OPTIONS: SelectOption[] = [
  { value: 'Dog', label: 'Perro' },
  { value: 'Cat', label: 'Gato' },
  { value: 'Bird', label: 'Ave (loro, canario…)' },
  { value: 'Rabbit', label: 'Conejo' },
  { value: 'Other', label: 'Otro' },
]

export const SEX_OPTIONS: SelectOption[] = [
  { value: 'Female', label: 'Hembra' },
  { value: 'Male', label: 'Macho' },
  { value: 'Unknown', label: 'Desconocido' },
]

export const SIZE_OPTIONS: SelectOption[] = [
  { value: 'Small', label: 'Pequeño' },
  { value: 'Medium', label: 'Mediano' },
  { value: 'Large', label: 'Grande' },
]
