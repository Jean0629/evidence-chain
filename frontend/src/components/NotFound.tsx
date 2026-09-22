import { Link } from 'react-router-dom'

export function NotFound() {
  return (
    <div className="py-16 text-center">
      <p className="text-slate-600">La página que buscas no existe.</p>
      <Link to="/evidence" className="mt-2 inline-block text-sm text-slate-900 underline">
        Volver a la bandeja
      </Link>
    </div>
  )
}
