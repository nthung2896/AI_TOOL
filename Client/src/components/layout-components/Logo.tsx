import { APP_NAME, APP_OWNER, APP_SUBTITLE } from "@/configs/AppConfig";
import {
  NAV_TYPE_TOP,
  SIDE_NAV_COLLAPSED_WIDTH,
  SIDE_NAV_WIDTH,
  TEMPLATE,
} from "@/constants/ThemeConstant";
import { useSelector } from "@/store/hooks";
import utils from "@/utils";
import styled from "@emotion/styled";
import { Grid, Image } from "antd";
import Link from "next/link";
import React from "react";

const LogoWrapper = styled.div(() => ({
  height: TEMPLATE.HEADER_HEIGHT,
  display: "flex",
  alignItems: "center",
  padding: "0 1rem",
  backgroundColor: "transparent",
  transition: "all .2s ease",
}));

const { useBreakpoint } = Grid;

interface LogoProps {
  mobileLogo?: boolean;
  logoType: "light" | "default";
}

// Component Logo
export const Logo: React.FC<LogoProps> = ({ mobileLogo, logoType }) => {
  const StaticFileUrl = process.env.NEXT_PUBLIC_STATIC_FILE_BASE_URL;
  const isMobile = !utils.getBreakPoint(useBreakpoint()).includes("lg");
  const navCollapsed = useSelector((state) => state.customizer.isCollapse);
  const navType = useSelector((state) => state.customizer.navType);

  const getLogoWidthGutter = (): string | number => {
    const isNavTop = navType === NAV_TYPE_TOP;

    if (isMobile && !mobileLogo) {
      return 0;
    }
    if (isNavTop) {
      return "auto";
    }
    if (navCollapsed) {
      return `${SIDE_NAV_COLLAPSED_WIDTH}px`;
    } else {
      return `${SIDE_NAV_WIDTH}px`;
    }
  };

  const getLogo = (): string => {
    if (logoType === "light") {
      return "/ai-tool.svg";
    }

    if (navCollapsed) {
      return "/ai-tool.svg";
    }
    return "/ai-tool.svg";
  };

  const splitTextIntoTwoParts = (text: string): React.ReactNode => {
    const words = text.trim().split(/\s+/);
    const totalWords = words.length;
    const mid = Math.floor(totalWords / 2);

    const firstPart = words.slice(0, mid).join(" ");
    const secondPart = words.slice(mid).join(" ");

    return (
      <>
        <div>{firstPart}</div>
        <div>{secondPart}</div>
      </>
    );
  };

  return (
    <LogoWrapper className={isMobile && !mobileLogo ? "d-none" : "logo"}>
      <Link href="/dashboard" className="flex items-center shrink-0 gap-2">
        <Image
          src={getLogo()}
          alt={`${APP_NAME} logo`}
          preview={false}
          style={{ width: "50px", height: "50px" }}
        />
        {!navCollapsed && (
          <div className="leading-tight font-sans tracking-tight w-[260px]">
            <div className="text-[11px] font-bold uppercase text-black/80">
              {APP_OWNER}
            </div>
            <div className="text-lg font-extrabold text-black">{APP_NAME}</div>
            <div className="text-[9px] font-semibold uppercase text-black/60 leading-snug">
              {APP_SUBTITLE}
            </div>
          </div>
        )}
      </Link>
    </LogoWrapper>
  );
};

export default Logo;
