import type { MouseEvent } from 'react'
import { activityLocationLabel } from '../utils/activity'
import { appleMapsUrl, googleMapsUrl, mapsSearchQuery } from '../utils/maps'

interface ActivityLocationLinkProps {
  location?: string
  meetingPoint?: string
  className?: string
  variant?: 'inline' | 'detail'
}

function stopBubble(e: MouseEvent) {
  e.stopPropagation()
}

export function ActivityLocationLink({
  location,
  meetingPoint,
  className = 'activity-location',
  variant = 'inline',
}: ActivityLocationLinkProps) {
  const label = activityLocationLabel(location, meetingPoint)
  const query = mapsSearchQuery(location, meetingPoint)
  if (!label || !query) return null

  if (variant === 'detail') {
    return (
      <div className="activity-location-block">
        <p className={className}>📍 {label}</p>
        <div className="map-links">
          <a
            href={googleMapsUrl(query)}
            target="_blank"
            rel="noopener noreferrer"
            className="map-link"
            onClick={stopBubble}
          >
            Google Maps
          </a>
          <a
            href={appleMapsUrl(query)}
            target="_blank"
            rel="noopener noreferrer"
            className="map-link"
            onClick={stopBubble}
          >
            Apple Maps
          </a>
        </div>
      </div>
    )
  }

  return (
    <div className={`activity-location-block activity-location-block--inline ${className}`}>
      <span className="activity-location-text">📍 {label}</span>
      <div className="map-links map-links--inline">
        <a
          href={googleMapsUrl(query)}
          target="_blank"
          rel="noopener noreferrer"
          className="map-link"
          onClick={stopBubble}
        >
          Google Maps
        </a>
        <span className="map-link-sep" aria-hidden="true">
          ·
        </span>
        <a
          href={appleMapsUrl(query)}
          target="_blank"
          rel="noopener noreferrer"
          className="map-link"
          onClick={stopBubble}
        >
          Apple Maps
        </a>
      </div>
    </div>
  )
}
