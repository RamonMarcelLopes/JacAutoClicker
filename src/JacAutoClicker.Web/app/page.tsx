'use client'

import { Keyboard, Maximize2, Minus, MousePointer2, RotateCcw, Settings2, X } from 'lucide-react'
import { send } from '@/lib/bridge'
import { useHostState } from '@/lib/use-host-state'

const pad = (value: number) => String(value).padStart(2, '0')

export default function Page() {
  const { running, capturingTrigger, clickCount, cps, triggerLabel, interval, clickButton, clickLimit } = useHostState()

  const updateInterval = (patch: Partial<typeof interval>) => send({ type: 'updateInterval', ...interval, ...patch })

  const stopDrag = (event: React.MouseEvent) => event.stopPropagation()

  return (
    <main className="h-screen w-screen overflow-hidden bg-transparent text-foreground">
      <section className="h-full w-full overflow-hidden rounded-[22px] border border-border/80 bg-card">
        <header onMouseDown={() => send({ type: 'startWindowDrag' })} className="border-b border-border/70 px-5 pb-4 pt-2">
          <div className="flex justify-end" aria-label="Controles da janela">
            <div className="flex items-center gap-1.5">
              <button aria-label="Fechar janela" title="Fechar" onMouseDown={stopDrag} onClick={() => send({ type: 'closeWindow' })} className="flex h-3 w-3 items-center justify-center rounded-full bg-[#ff5f57] text-[#681b17] transition hover:brightness-110"><X className="h-2 w-2 opacity-0 hover:opacity-100" /></button>
              <button aria-label="Minimizar janela" title="Minimizar" onMouseDown={stopDrag} onClick={() => send({ type: 'minimizeWindow' })} className="flex h-3 w-3 items-center justify-center rounded-full bg-[#febc2e] text-[#6b4300] transition hover:brightness-110"><Minus className="h-2 w-2 opacity-0 hover:opacity-100" /></button>
              <button aria-label="Tela cheia desativada" title="Tela cheia indisponível" onMouseDown={stopDrag} disabled className="flex h-3 w-3 cursor-not-allowed items-center justify-center rounded-full bg-[#28c840]/40 text-[#123b18]"><Maximize2 className="h-2 w-2" /></button>
            </div>
          </div>
          <div className="mt-2 flex items-center justify-between gap-3">
            <div className="flex min-w-0 items-center gap-3">
              <div className="flex h-[56px] w-[56px] shrink-0 items-center justify-center">
                <img src="/jaca-icon.png" alt="" className="h-full w-full object-contain" draggable={false} />
              </div>
              <div>
                <h1 className="font-mono text-[15px] font-bold tracking-tight">Jacaclicker</h1>
                <p className="mt-0.5 text-[10px] uppercase tracking-[0.18em] text-muted-foreground">click utility</p>
              </div>
            </div>
            <div onMouseDown={stopDrag} className="flex items-center gap-2 rounded-full border border-border bg-background px-3 py-1.5">
              <span className={`h-1.5 w-1.5 rounded-full ${running ? 'bg-emerald-400 shadow-[0_0_8px_#34d399]' : 'bg-muted-foreground/50'}`} />
              <span className="text-[11px] font-medium text-muted-foreground">{running ? 'Rodando' : 'Parado'}</span>
            </div>
          </div>
        </header>

        <div className="space-y-4 p-5">
          <div className="grid grid-cols-2 gap-3">
            <div className="rounded-2xl border border-border bg-background/70 p-4">
              <p className="text-[10px] font-semibold uppercase tracking-[0.15em] text-muted-foreground">CPS atual</p>
              <p className="mt-2 font-mono text-3xl font-bold tracking-tight text-primary">{cps.toFixed(1)}</p>
              <p className="mt-1 text-[11px] text-muted-foreground">cliques / segundo</p>
            </div>
            <div className="rounded-2xl border border-border bg-background/70 p-4">
              <p className="text-[10px] font-semibold uppercase tracking-[0.15em] text-muted-foreground">Cliques dados</p>
              <p className="mt-2 font-mono text-3xl font-bold tracking-tight">{clickCount.toLocaleString('pt-BR')}</p>
              <p className="mt-1 text-[11px] text-muted-foreground">{clickLimit > 0 ? `limite: ${clickLimit}` : 'sem limite definido'}</p>
            </div>
          </div>

          <div className="rounded-2xl border border-border bg-background/70 p-4">
            <div className="mb-3 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Keyboard className="h-4 w-4 text-primary" />
                <h2 className="text-xs font-semibold">Tecla de ativação</h2>
              </div>
              <span className="text-[10px] text-muted-foreground">pressione para alternar</span>
            </div>
            <div className="flex gap-2">
              <div className="flex h-10 min-w-0 flex-1 items-center rounded-xl border border-input bg-background px-3 font-mono text-sm font-semibold">
                {capturingTrigger ? (
                  <span className="truncate text-xs font-normal italic text-amber-400">Pressione uma tecla ou clique do mouse... [ESC cancela]</span>
                ) : (
                  triggerLabel
                )}
              </div>
              <button
                onClick={() => send({ type: 'startTriggerCapture' })}
                disabled={capturingTrigger || running}
                className="h-10 rounded-xl border border-border px-4 text-xs font-medium text-muted-foreground transition hover:bg-accent hover:text-foreground disabled:pointer-events-none disabled:opacity-50"
              >
                Alterar
              </button>
            </div>
          </div>

          <div className="rounded-2xl border border-border bg-background/70 p-4">
            <div className="mb-3 flex items-center justify-between">
              <div className="flex items-center gap-2"><Settings2 className="h-4 w-4 text-primary" /><h2 className="text-xs font-semibold">Intervalo entre cliques</h2></div>
              <span className="font-mono text-[11px] text-muted-foreground">{interval.hours}h {pad(interval.minutes)}m {pad(interval.seconds)}s {interval.milliseconds}ms</span>
            </div>
            <div className="grid grid-cols-4 gap-2">
              {(
                [
                  ['hr', interval.hours, 23, (v: number) => updateInterval({ hours: v })],
                  ['min', interval.minutes, 59, (v: number) => updateInterval({ minutes: v })],
                  ['seg', interval.seconds, 59, (v: number) => updateInterval({ seconds: v })],
                  ['ms', interval.milliseconds, 999, (v: number) => updateInterval({ milliseconds: v })],
                ] as const
              ).map(([label, value, max, setter]) => (
                <label key={label} className="space-y-1.5">
                  <span className="block text-[10px] uppercase tracking-wider text-muted-foreground">{label}</span>
                  <input
                    type="number"
                    min="0"
                    max={max}
                    value={value}
                    onChange={(event) => setter(Math.max(0, Math.min(max, Number(event.target.value))))}
                    className="h-9 w-full rounded-lg border border-input bg-background px-2 font-mono text-xs outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
                  />
                </label>
              ))}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="rounded-2xl border border-border bg-background/70 p-4">
              <div className="mb-3 flex items-center gap-2"><MousePointer2 className="h-4 w-4 text-primary" /><h2 className="text-xs font-semibold">Botão do mouse</h2></div>
              <div className="flex gap-1 rounded-xl bg-muted p-1">
                <button onClick={() => send({ type: 'updateClickButton', value: 'left' })} className={`flex-1 rounded-lg py-2 text-xs font-medium transition ${clickButton === 'left' ? 'bg-card text-foreground shadow-sm' : 'text-muted-foreground'}`}>Esquerdo</button>
                <button onClick={() => send({ type: 'updateClickButton', value: 'right' })} className={`flex-1 rounded-lg py-2 text-xs font-medium transition ${clickButton === 'right' ? 'bg-card text-foreground shadow-sm' : 'text-muted-foreground'}`}>Direito</button>
              </div>
            </div>
            <label className="rounded-2xl border border-border bg-background/70 p-4">
              <span className="mb-3 block text-xs font-semibold">Limite de cliques</span>
              <input
                type="number"
                min="0"
                value={clickLimit}
                onChange={(event) => send({ type: 'updateClickLimit', value: Math.max(0, Number(event.target.value)) })}
                className="h-9 w-full rounded-lg border border-input bg-background px-3 font-mono text-sm outline-none focus:border-primary focus:ring-2 focus:ring-primary/20"
              />
              <span className="mt-2 block text-[10px] text-muted-foreground">0 = ilimitado</span>
            </label>
          </div>

          <div className="flex gap-2 pt-1">
            <button
              onClick={() => send({ type: 'toggleClicking' })}
              className={`flex h-11 flex-1 items-center justify-center gap-2 rounded-xl text-sm font-semibold transition active:scale-[0.98] ${running ? 'bg-destructive text-destructive-foreground hover:bg-destructive/90' : 'bg-primary text-primary-foreground hover:bg-primary/90'}`}
            >
              <span className={`h-2 w-2 rounded-full ${running ? 'bg-destructive-foreground' : 'bg-primary-foreground'}`} />
              {running ? 'Parar autoclick' : 'Iniciar autoclick'}
            </button>
            <button aria-label="Zerar contagem" onClick={() => send({ type: 'resetCount' })} className="flex h-11 w-11 items-center justify-center rounded-xl border border-border text-muted-foreground transition hover:bg-accent hover:text-foreground">
              <RotateCcw className="h-4 w-4" />
            </button>
          </div>
          <p className="text-center text-[10px] text-muted-foreground">Atalho ativo: <kbd className="rounded border border-border bg-muted px-1.5 py-0.5 font-mono text-[10px] text-foreground">{triggerLabel || '—'}</kbd></p>
        </div>
      </section>
    </main>
  )
}
