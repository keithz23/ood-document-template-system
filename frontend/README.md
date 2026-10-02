# Frontend

This directory contains the Next.js App Router client for Document Template
System. It uses TypeScript, Tailwind CSS, shadcn/ui, TanStack Query, Axios,
React Hook Form, and Zod. A shared TipTap component powers Draft Admin
TemplateVersion editing and Draft author Document editing; Published versions
and Finalized Documents use its read-only mode.

## Local commands

```bash
cp .env.example .env.local
npm install
npm run dev
npm run lint
npm run build
npm run start
```

`NEXT_PUBLIC_API_URL` must point to the API base path; the local default is
`http://localhost:5000/api`. Full setup, demo credentials, architecture, and
deployment notes are in the [repository README](../README.md).
