"use client";
import { useCallback, useEffect, useState } from "react";
import { Menu } from "antd";
import { AppstoreOutlined, HomeOutlined, EditOutlined, TeamOutlined, MessageOutlined, CalendarOutlined } from "@ant-design/icons";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useDispatch, useSelector } from "@/store/hooks";
import { setIsMobile } from "@/store/customizer/CustomizerSlice";
import { accountApi } from "@/app/rental-assistant/CustomerSettings";
import type { Administration } from "@/services/rentalCustomer/rentalCustomer.service";

const icons: Record<string, React.ReactNode> = {
 AppstoreOutlined: <AppstoreOutlined />, HomeOutlined: <HomeOutlined />, EditOutlined: <EditOutlined />,
 TeamOutlined: <TeamOutlined />, MessageOutlined: <MessageOutlined />, CalendarOutlined: <CalendarOutlined />,
};
export default function MenuContent() {
 const pathname = usePathname();
 const [hash, setHash] = useState("#overview");
 const [menu, setMenu] = useState<Administration | null>(null);
 const dispatch = useDispatch();
 const collapsed = useSelector(state => state.customizer.isCollapse);
 const refresh = useCallback(() => {
   void accountApi("administration").then(setMenu).catch(() => setMenu(null));
 }, []);
 useEffect(() => { refresh(); }, [pathname, refresh]);
 useEffect(() => {
   const sync = () => setHash(window.location.hash || "#overview");
   sync(); window.addEventListener("hashchange", sync);
   window.addEventListener("rental-menu-changed", refresh);
   window.addEventListener("focus", refresh);
   return () => { window.removeEventListener("hashchange", sync); window.removeEventListener("rental-menu-changed", refresh); window.removeEventListener("focus", refresh); };
 }, [refresh]);
 const selected = pathname === "/dashboard" ? pathname + hash : pathname;
 return <Menu mode="inline" theme="light" className="left-menu" inlineCollapsed={collapsed} selectedKeys={menu?.entries.filter(entry => entry.url === selected).map(entry => entry.id) || []}
 items={(menu?.modules || []).map(module => ({
   key: module.id, label: module.name, type: "group" as const,
   children: module.entries.map(entry => ({
     key: entry.id, icon: icons[entry.icon || ""] || <AppstoreOutlined />,
     label: <Link href={entry.url} onClick={event => {
       const target = new URL(entry.url, window.location.origin);
       if (target.origin === window.location.origin && target.pathname === pathname && target.hash && !event.ctrlKey && !event.metaKey && !event.shiftKey && !event.altKey) {
         event.preventDefault(); window.location.hash = target.hash;
       }
       dispatch(setIsMobile(false));
     }}>{entry.name}</Link>,
   })),
 }))} />;
}
