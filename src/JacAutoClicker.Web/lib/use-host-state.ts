'use client'

import { useEffect, useState } from 'react'
import { subscribe, type HostState } from './bridge'

const DEFAULT_STATE: HostState = {
  running: false,
  capturingTrigger: false,
  clickCount: 0,
  cps: 0,
  triggerLabel: 'F6',
  interval: { hours: 0, minutes: 0, seconds: 0, milliseconds: 100 },
  clickButton: 'left',
  clickLimit: 0,
}

export function useHostState(): HostState {
  const [state, setState] = useState<HostState>(DEFAULT_STATE)
  useEffect(() => subscribe(setState), [])
  return state
}
