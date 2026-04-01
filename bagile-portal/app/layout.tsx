import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "BAgile Portal",
  description: "BAgile Platform Dashboard",
  icons: {
    icon: "/favicon.png",
  },
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en">
      <head />
      <body className="bg-gray-50 min-h-screen">{children}</body>
    </html>
  );
}
