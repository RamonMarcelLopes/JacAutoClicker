/** @type {import('next').NextConfig} */
const nextConfig = {
  output: 'export',
  // Keeps the _next/static/<id>/ folder name stable across builds instead of a fresh random id
  // every time, so the .csproj's wwwroot Content items don't go stale mid-build.
  generateBuildId: () => 'static',
  typescript: {
    ignoreBuildErrors: true,
  },
  images: {
    unoptimized: true,
  },
}

export default nextConfig
