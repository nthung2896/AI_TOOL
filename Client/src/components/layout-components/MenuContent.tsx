"use client";
import { useEffect, useState } from "react";
import { Menu } from "antd";
import { AppstoreOutlined, HomeOutlined, EditOutlined, TeamOutlined, MessageOutlined, CalendarOutlined } from "@ant-design/icons";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useDispatch, useSelector } from "@/store/hooks";
import { setIsMobile } from "@/store/customizer/CustomizerSlice";
const entries = [
 { key: "overview", title: "Tổng quan", icon: <AppstoreOutlined /> },
 { key: "rooms", title: "Quản lý phòng trọ", icon: <HomeOutlined /> },
 { key: "posts", title: "Soạn & đăng bài", icon: <EditOutlined /> },
 { key: "groups", title: "Nhóm Facebook", icon: <TeamOutlined /> },
 { key: "threads", title: "Khách & Messenger", icon: <MessageOutlined /> },
 { key: "appointments", title: "Lịch xem phòng", icon: <CalendarOutlined /> },
];
export default function MenuContent() {
 const [active, setActive] = useState("overview");
 const pathname = usePathname(); const dispatch = useDispatch();
 const collapsed = useSelector(state => state.customizer.isCollapse);
 useEffect(() => { const sync = () => setActive(window.location.hash.slice(1) || "overview"); sync(); window.addEventListener("hashchange", sync); return () => window.removeEventListener("hashchange", sync); }, []);
 return <Menu mode="inline" theme="light" className="left-menu" inlineCollapsed={collapsed} selectedKeys={pathname === "/dashboard" ? [active] : []}
 items={entries.map(entry => ({ key: entry.key, icon: entry.icon, label: <Link href={"/dashboard#" + entry.key} onClick={(event) => {
   if (pathname === "/dashboard" && !event.ctrlKey && !event.metaKey && !event.shiftKey && !event.altKey) {
     event.preventDefault();
     window.location.hash = entry.key;
   }
   dispatch(setIsMobile(false));
 }}>{entry.title}</Link> }))} />;
}
