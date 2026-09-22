import { useState, type FormEvent, type RefObject } from 'react'
import type { Custodian } from '../../api/types'
import { useCustodians } from '../../api/useCustodians'
import { Modal } from '../../components/Modal'
import { custodianOptionLabel } from '../../lib/format'

interface TransferModalProps {
  evidenceCode: string
  currentCustodianId: string
  onConfirm: (toCustodian: Custodian) => void
  onClose: () => void
  returnFocusRef: RefObject<HTMLElement | null>
}

export function TransferModal({
  evidenceCode,
  currentCustodianId,
  onConfirm,
  onClose,
  returnFocusRef,
}: TransferModalProps) {
  const custodians = useCustodians()
  const [selectedId, setSelectedId] = useState('')

  const options = custodians.data?.filter((c) => c.role === 'Custodio' && c.id !== currentCustodianId) ?? []
  const selected = options.find((c) => c.id === selectedId)

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (selected) onConfirm(selected)
  }

  return (
    <Modal title="Transferir custodia" onClose={onClose} returnFocusRef={returnFocusRef}>
      <form onSubmit={handleSubmit} className="mt-4 space-y-4">
        <p className="text-sm text-slate-600">
          Selecciona quién recibirá la custodia de <span className="font-mono font-medium">{evidenceCode}</span>. El
          destinatario deberá aceptar la transferencia.
        </p>

        <div>
          <label htmlFor="toCustodian" className="block text-sm font-medium text-slate-700">
            Custodio destino
          </label>
          {custodians.isPending ? (
            <p className="mt-1 text-sm text-slate-500">Cargando custodios…</p>
          ) : custodians.isError ? (
            <div role="alert" className="mt-1 text-sm text-red-700">
              No se pudo cargar la lista de custodios.{' '}
              <button type="button" onClick={() => void custodians.refetch()} className="underline">
                Reintentar
              </button>
            </div>
          ) : (
            <select
              id="toCustodian"
              data-autofocus
              value={selectedId}
              onChange={(e) => setSelectedId(e.target.value)}
              className="mt-1 block w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm focus:border-slate-500 focus:outline-none focus:ring-1 focus:ring-slate-500"
            >
              <option value="">Selecciona un custodio…</option>
              {options.map((c) => (
                <option key={c.id} value={c.id}>
                  {custodianOptionLabel(c)}
                </option>
              ))}
            </select>
          )}
        </div>

        <div className="flex justify-end gap-2 pt-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded-md border border-slate-300 px-4 py-2 text-sm text-slate-700 hover:bg-slate-50"
          >
            Cancelar
          </button>
          <button
            type="submit"
            disabled={!selected}
            className="rounded-md bg-slate-900 px-4 py-2 text-sm font-medium text-white hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-50"
          >
            Confirmar transferencia
          </button>
        </div>
      </form>
    </Modal>
  )
}
