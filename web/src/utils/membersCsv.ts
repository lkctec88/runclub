export const MEMBERS_CSV_HEADERS = [
  'firstName',
  'lastName',
  'englandAthleticsNumber',
  'role',
  'status',
] as const

export const MEMBERS_CSV_TEMPLATE = [
  MEMBERS_CSV_HEADERS.join(','),
  'Jane,Doe,1234567,Member,Active',
  'Alex,Admin,7654321,Admin,Active',
  'Pat,Lee,1122334,SuperAdmin,Active',
  'Sam,Runner,9999999,Member,Lapsed',
  '',
].join('\r\n')

export function downloadMembersCsvTemplate() {
  const blob = new Blob([`\uFEFF${MEMBERS_CSV_TEMPLATE}`], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = 'members-template.csv'
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}
