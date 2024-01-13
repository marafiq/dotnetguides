import { Button } from '@/components/Button'
import { Heading } from '@/components/Heading'

const guides = [
  {
    href: '/authentication',
    name: 'Modern C# Features',
    description: 'Learn the essential C# features to write concise and expressive code. Starting from C# 7.2 to C# 12, a lot of quality features has been shipped.',
  },
  {
    href: '/pagination',
    name: 'Configurations in .NET 8',
    description: 'Configurations is the first concept you must learn to build and deploy secure .NET 8 applications. Yes, the Options Patterns.',
  },
  {
    href: '/errors',
    name: 'Host and HostBuilder Pattern in .NET 8',
    description:
      'Every .NET 8 application starts with a host, and it uses the host builder pattern to configure and build itself.',
  },
  {
    href: '/webhooks',
    name: 'Middleware in .NET 8',
    description:
      'The HTTP Request and Response pipeline in .NET 8 is built using middleware understand it by building a custom middleware pipeline.',
  },
]

export function Guides() {
  return (
    <div className="my-16 xl:max-w-none">
      <Heading level={2} id="guides">
        Guides
      </Heading>
      <div className="not-prose mt-4 grid grid-cols-1 gap-8 border-t border-zinc-900/5 pt-10 dark:border-white/5 sm:grid-cols-2 xl:grid-cols-4">
        {guides.map((guide) => (
          <div key={guide.href}>
            <h3 className="text-sm font-semibold text-zinc-900 dark:text-white">
              {guide.name}
            </h3>
            <p className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
              {guide.description}
            </p>
            <p className="mt-4">
              <Button href={guide.href} variant="text" arrow="right">
                Read more
              </Button>
            </p>
          </div>
        ))}
      </div>
    </div>
  )
}
