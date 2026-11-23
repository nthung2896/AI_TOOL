"use client";

import LoginForm from "@/components/auth-components/LoginForm";
import { CalendarOutlined, HomeOutlined, MessageOutlined, ThunderboltOutlined } from "@ant-design/icons";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import React, { useEffect } from "react";
import "./login.css";

const features = [
  { icon: <HomeOutlined />, title: "Quản lý phòng trọ", description: "Theo dõi thông tin phòng, giá thuê và tình trạng còn trống." },
  { icon: <MessageOutlined />, title: "Nội dung & khách hàng", description: "Soạn bài cho thuê, quản lý nhóm Facebook và hội thoại Messenger." },
  { icon: <CalendarOutlined />, title: "Lịch xem phòng", description: "Theo dõi lịch hẹn và điều phối trợ lý qua Telegram." },
];

const Login: React.FC = () => {
  const params = useSearchParams();
  const redirectUrl = params?.get("redirect_url");

  useEffect(() => {
    if (redirectUrl) localStorage.setItem("redirect_url", redirectUrl);
  }, [redirectUrl]);

  return (
    <main className="ai-login">
      <section className="ai-login-card" aria-label="Đăng nhập AI Tool">
        <div className="ai-login-intro">
          <div className="ai-login-brand"><span className="ai-login-mark"><ThunderboltOutlined /></span> AI TOOL</div>
          <div>
            <span className="ai-login-eyebrow">TRỢ LÝ CHO THUÊ PHÒNG</span>
            <h2>Quản lý tập trung.<br />Làm việc hiệu quả hơn.</h2>
            <p className="ai-login-description">Không gian quản lý phòng trọ, nội dung cho thuê và chăm sóc khách hàng với sự hỗ trợ của trợ lý AI.</p>
            <div className="ai-login-features">
              {features.map(({ icon, title, description }) => (
                <div className="ai-login-feature" key={title}>
                  <span className="ai-login-feature-icon">{icon}</span>
                  <div><h3>{title}</h3><p>{description}</p></div>
                </div>
              ))}
            </div>
          </div>
          <span className="ai-login-caption">AI Tool · Trợ lý đồng hành cùng công việc của bạn</span>
        </div>
        <div className="ai-login-form-panel">
          <div className="ai-login-form-heading">
            <span className="ai-login-eyebrow">KHÔNG GIAN LÀM VIỆC</span>
            <h1>Đăng nhập AI Tool</h1>
            <p>Sử dụng tài khoản được cấp để truy cập hệ thống.</p>
          </div>
          <LoginForm />
          <div className="ai-login-rental-link">
            <span>Sử dụng khóa quản lý trợ lý?</span>
            <Link href="/rental-assistant">Mở bảng điều khiển cho thuê phòng →</Link>
          </div>
        </div>
      </section>
    </main>
  );
};

export default Login;
