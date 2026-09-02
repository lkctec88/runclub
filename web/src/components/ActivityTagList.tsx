export function ActivityTagList({ tags }: { tags?: string[] | null }) {
  if (!tags?.length) return null
  return (
    <ul className="activity-tags">
      {tags.map((tag) => (
        <li key={tag} className="badge badge-tag">
          {tag}
        </li>
      ))}
    </ul>
  )
}
