'use client'

export type HostState = {
  running: boolean
  capturingTrigger: boolean
  clickCount: number
  cps: number
  triggerLabel: string
  interval: { hours: number; minutes: number; seconds: number; milliseconds: number }
  clickButton: 'left' | 'right'
  clickLimit: number
}

type Listener = (state: HostState) => void

declare global {
  interface Window {
    chrome?: {
      webview?: {
        postMessage: (message: unknown) => void
      }
    }
    __hostBridge?: {
      receive: (state: HostState) => void
    }
  }
}

const listeners = new Set<Listener>()

if (typeof window !== 'undefined') {
  window.__hostBridge = {
    receive(state: HostState) {
      listeners.forEach((listener) => listener(state))
    },
  }
}

export function subscribe(listener: Listener): () => void {
  listeners.add(listener)
  return () => listeners.delete(listener)
}

export function send(message: Record<string, unknown>) {
  window.chrome?.webview?.postMessage(message)
}
