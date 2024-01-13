export function Logo(props: React.ComponentPropsWithoutRef<'svg'>) {
  return (
      <div className="flex flex-row">
          <svg viewBox="0 0 22 24" aria-hidden="true" {...props}>
              <path
                  className="fill-emerald-400"
                  d="M16 8a5 5 0 0 0-5-5H5a5 5 0 0 0-5 5v13.927a1 1 0 0 0 1.623.782l3.684-2.93a4 4 0 0 1 2.49-.87H11a5 5 0 0 0 5-5V8Z"
              />

          </svg>
          <span className="fill-zinc-900 dark:fill-white font-bold">.NET Guides</span>
      </div>

  )
}
