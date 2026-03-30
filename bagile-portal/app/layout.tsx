import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "BAgile Portal",
  description: "BAgile Platform Dashboard",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <head>
        <script
          src="https://accounts.google.com/gsi/client"
          async
          defer
        />
      </head>
      <body className="bg-gray-50 min-h-screen">{children}</body>
    </html>
  );
}
