"use client";
import { lightTheme } from "@/constants/ThemeConstant";

import { Providers } from "@/store/providers";
import { App as AntdApp, ConfigProvider } from "antd";
import viVN from "antd/locale/vi_VN";
import React from "react";
import "./global.css";

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="vi" suppressHydrationWarning>
      <head>
        <meta charSet="utf-8" />
        <meta
          name="viewport"
          content="width=device-width, initial-scale=1, shrink-to-fit=no"
        />
        <link rel="icon" type="image/svg+xml" href="/ai-tool.svg" />
        <link rel="shortcut icon" href="/ai-tool.svg" />
        <title>AI Tool - Trợ lý cho thuê phòng</title>
      </head>
      <body className="dir-ltr">
        <div className="App">
          <Providers>
            <ConfigProvider theme={lightTheme} locale={viVN} componentSize="middle">
              <AntdApp>{children}</AntdApp>
            </ConfigProvider>
          </Providers>
        </div>
      </body>
    </html>
  );
}

