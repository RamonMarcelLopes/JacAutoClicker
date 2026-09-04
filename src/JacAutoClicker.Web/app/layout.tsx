import type { Metadata, Viewport } from 'next'
import { Geist, Geist_Mono } from 'next/font/google'
import './globals.css'

const geist = Geist({ subsets: ['latin'], variable: '--font-geist' })
const geistMono = Geist_Mono({ subsets: ['latin'], variable: '--font-geist-mono' })

export const metadata: Metadata = {
  title: 'Jacaclicker — Auto Clicker',
  description: 'Controle compacto para automação de cliques.',
}

export const viewport: Viewport = {
  colorScheme: 'dark',
  themeColor: '#111820',
  userScalable: false,
}

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="pt-BR" className="bg-background">
      <body className={`${geist.variable} ${geistMono.variable}`}>{children}</body>
    </html>
  )
}
