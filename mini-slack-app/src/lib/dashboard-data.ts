export interface DashboardChannel {
  id: string
  name: string
  unread: number
  isPrivate?: boolean
}

export interface DashboardDm {
  id: string
  name: string
  status: 'online' | 'away' | 'offline'
  unread: number
}

export interface DashboardMessage {
  id: string
  author: string
  avatar: string
  time: string
  body: string
}

export const channels: DashboardChannel[] = [
  { id: 'general', name: 'general', unread: 4 },
  { id: 'product', name: 'product', unread: 0 },
  { id: 'engineering', name: 'engineering', unread: 2 },
  { id: 'design-review', name: 'design-review', unread: 0, isPrivate: true },
  { id: 'customer-feedback', name: 'customer-feedback', unread: 1 },
]

export const directMessages: DashboardDm[] = [
  { id: 'an', name: 'An Nguyen', status: 'online', unread: 0 },
  { id: 'linh', name: 'Linh Tran', status: 'away', unread: 3 },
  { id: 'minh', name: 'Minh Pham', status: 'online', unread: 0 },
  { id: 'sarah', name: 'Sarah Lee', status: 'offline', unread: 0 },
]

export const messagesByChannel: Record<string, DashboardMessage[]> = {
  general: [
    {
      id: 'm1',
      author: 'An Nguyen',
      avatar: 'AN',
      time: '09:12',
      body: 'Morning. The Google login flow is stable now, so I started mapping the dashboard states we need next.',
    },
    {
      id: 'm2',
      author: 'Linh Tran',
      avatar: 'LT',
      time: '09:18',
      body: 'I added notes for empty channels, unread badges, and mobile sidebar behavior. The shell can support all of those.',
    },
    {
      id: 'm3',
      author: 'Minh Pham',
      avatar: 'MP',
      time: '09:24',
      body: 'Let us keep the first version focused: channels, messages, composer, and the authenticated user menu.',
    },
  ],
  product: [
    {
      id: 'p1',
      author: 'Sarah Lee',
      avatar: 'SL',
      time: '10:03',
      body: 'The first dashboard should make workspace activity obvious without adding reporting widgets too early.',
    },
  ],
  engineering: [
    {
      id: 'e1',
      author: 'Minh Pham',
      avatar: 'MP',
      time: '10:21',
      body: 'API integration can follow once we have stable channel and message contracts.',
    },
    {
      id: 'e2',
      author: 'An Nguyen',
      avatar: 'AN',
      time: '10:33',
      body: 'Agreed. I would keep mock data isolated so replacing it with a Pinia-backed workspace store is direct.',
    },
  ],
  'design-review': [
    {
      id: 'd1',
      author: 'Linh Tran',
      avatar: 'LT',
      time: '11:06',
      body: 'The dashboard uses quiet contrast, compact controls, and predictable scan patterns for repeated use.',
    },
  ],
  'customer-feedback': [
    {
      id: 'c1',
      author: 'Sarah Lee',
      avatar: 'SL',
      time: '11:42',
      body: 'A few beta users asked for a cleaner channel list and faster access to direct messages.',
    },
  ],
}
