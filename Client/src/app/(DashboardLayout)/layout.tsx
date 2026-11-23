"use client";
import { HeaderNav } from "@/components/layout-components/HeaderNav";
import { SideNav } from "@/components/layout-components/SideNav";
import Footer from "@/components/layout-components/Footer";
import Loading from "@/components/shared-components/Loading";
import { MEDIA_QUERIES, TEMPLATE } from "@/constants/ThemeConstant";
import authService from "@/services/auth/auth.service";
import { setUserInfo } from "@/store/auth/AuthSlice";
import { useSelector } from "@/store/hooks";
import { setMenuData } from "@/store/menu/MenuSlice";
import { AppDispatch } from "@/store/store";
import { AppUserType } from "@/types/appUser/dto";
import { MenuDataType } from "@/types/operation/dto";
import utils from "@/utils";
import styled from "@emotion/styled";
import { ConfigProvider, Grid, Layout } from "antd";
import locale from "antd/locale/vi_VN";
import dayjs from "dayjs";
import "dayjs/locale/vi";
import NProgress from "nprogress";
import { usePathname, useSearchParams } from "next/navigation";
import { Suspense, useEffect } from "react";
import { useDispatch } from "react-redux";

import "@/app/assets/css/global.css";
import "nprogress/nprogress.css";
import "./layout.css";

dayjs.locale("vi");

NProgress.configure({ showSpinner: false, trickleSpeed: 120, minimum: 0.15 });

const { useBreakpoint } = Grid;
const { Content } = Layout;

const AppContent = styled("div")`
  padding: ${TEMPLATE.LAYOUT_CONTENT_GUTTER}px;
  margin-top: ${TEMPLATE.HEADER_HEIGHT}px;
  min-height: calc(100vh - 126px);
  position: relative;
  @media ${MEDIA_QUERIES.MOBILE} {
    padding: ${TEMPLATE.LAYOUT_CONTENT_GUTTER_SM}px;
  }
`;

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const dispatch = useDispatch<AppDispatch>();
  const navCollapsed = useSelector((state) => state.customizer.isCollapse);
  const screens = utils.getBreakPoint(useBreakpoint());
  const isMobile = screens.length === 0 ? false : !screens.includes("lg");
  const userInfo: AppUserType | null = useSelector(
    (state: any) => state.auth.User,
  );
  const menuData: MenuDataType[] | null = useSelector(
    (state) => state.menu.menuData,
  );

  const getLayoutGutter = () => {
    if (isMobile) return 0;
    return navCollapsed
      ? TEMPLATE.SIDE_NAV_COLLAPSED_WIDTH
      : TEMPLATE.SIDE_NAV_WIDTH;
  };

  const handleGetUserInfo = async () => {
    try {
      const response = await authService.getInfo();
      if (response) {
        dispatch(setUserInfo(response));
        dispatch(setMenuData(response));
      }
    } catch (error) {
      console.log(error);
    }
  };

  useEffect(() => {
    if (userInfo == null || menuData == null) {
      handleGetUserInfo();
    }
  }, []);

  // NProgress bar khi đổi route (pathname / query).
  const pathname = usePathname();
  const searchParams = useSearchParams();
  useEffect(() => {
    NProgress.start();
    const t = setTimeout(() => NProgress.done(), 250);
    return () => {
      clearTimeout(t);
      NProgress.done();
    };
  }, [pathname, searchParams]);

  return (
    <ConfigProvider locale={locale}>
      <Layout>
        <HeaderNav />
        <SideNav />
        <Layout style={{ paddingLeft: getLayoutGutter() }}>
          <AppContent>
            <Content className="h-min-100">
              <Suspense fallback={<Loading content="content" />}>
                {children}
              </Suspense>
            </Content>
          </AppContent>
          <Footer />
        </Layout>
      </Layout>
    </ConfigProvider>
  );
}
