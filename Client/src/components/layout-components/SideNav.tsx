"use client";
import { SIDE_NAV_WIDTH, TEMPLATE } from "@/constants/ThemeConstant";
import {
  toggleMobileSidebar,
  toggleSidebar,
} from "@/store/customizer/CustomizerSlice";
import { useSelector } from "@/store/hooks";
import { AppDispatch } from "@/store/store";
import { MenuOutlined, SwapLeftOutlined } from "@ant-design/icons";
import { Layout } from "antd";
import { Scrollbars } from "react-custom-scrollbars-2";
import { useDispatch } from "react-redux";
import MenuContent from "./MenuContent";
const { Sider } = Layout;

export const SideNav = () => {
  const dispatch = useDispatch<AppDispatch>();
  const navCollapsed = useSelector((state) => state.customizer.isCollapse);
  const isMobile = useSelector((state) => state.customizer.isMobile);
  const onToggle = () => {
    if (!isMobile) {
      dispatch(toggleSidebar());
    } else {
      dispatch(toggleMobileSidebar());
    }
  };
  return (
    <Sider
      style={{
        height: `calc(100vh - ${TEMPLATE.HEADER_HEIGHT}px)`,
        position: `fixed`,
        top: `${TEMPLATE.HEADER_HEIGHT}px`,
        zIndex: 999,
        backgroundColor: `white`,
      }}
      className={`"side-nav border-r border-gray-200 ${
        navCollapsed ? "menu-min" : "menu-max"
      }`}
      width={SIDE_NAV_WIDTH}
      collapsed={navCollapsed}
    >
      <Scrollbars autoHide>
        <div className="p-2">
          <div
            className="flex gap-1 align-items-center hover bg-primary text-white p-[10px] rounded-sm cursor-pointer toggle-button"
            onClick={onToggle}
          >
            <MenuOutlined />
            {!navCollapsed && (
              <>
                <div className="mr-auto">Chức năng</div>
                <div>
                  <SwapLeftOutlined />
                </div>
              </>
            )}
          </div>
          <MenuContent />
        </div>
      </Scrollbars>
    </Sider>
  );
};
