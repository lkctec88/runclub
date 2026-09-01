export const DEFAULT_CLUB_LOGO = '/clubs/default-logo.jpg?v=3'

export function clubLogoUrl(logoUrl?: string | null) {
  return logoUrl?.trim() || DEFAULT_CLUB_LOGO
}
