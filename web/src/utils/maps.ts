export function mapsSearchQuery(location?: string, meetingPoint?: string) {
  if (location && meetingPoint && location !== meetingPoint) {
    return `${location}, ${meetingPoint}`
  }
  return location ?? meetingPoint ?? null
}

export function googleMapsUrl(query: string) {
  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(query)}`
}

export function appleMapsUrl(query: string) {
  return `https://maps.apple.com/?q=${encodeURIComponent(query)}`
}

export function isAppleDevice() {
  if (typeof navigator === 'undefined') return false
  return /iPad|iPhone|iPod|Mac/i.test(navigator.userAgent)
}

export function preferredMapsUrl(query: string) {
  return isAppleDevice() ? appleMapsUrl(query) : googleMapsUrl(query)
}
