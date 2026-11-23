"use client";
import { Result, Spin } from "antd";
import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";

const SsoCallback: React.FC = () => {
  const router = useRouter();
  const params = useSearchParams();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const token = params?.get("token");
    const err = params?.get("error");

    if (err) {
      setError(err);
      return;
    }
    if (!token) {
      setError("Không nhận được token từ Cổng dịch vụ công");
      return;
    }

    // Lưu JWT giống flow login thường (xem AuthSlice.setLogin)
    localStorage.setItem("AccessToken", token);

    const redirect = localStorage.getItem("redirect_url");
    if (redirect) {
      localStorage.removeItem("redirect_url");
      router.replace(redirect);
    } else {
      router.replace("/dashboard");
    }
  }, [params, router]);

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-[#f3f4f8]">
        <Result
          status="error"
          title="Đăng nhập SSO thất bại"
          subTitle={error}
          extra={
            <a href="/auth/login" className="text-blue-600 underline">
              Quay lại trang đăng nhập
            </a>
          }
        />
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-[#f3f4f8]">
      <Spin tip="Đang xử lý đăng nhập SSO..." size="large">
        <div style={{ minWidth: 240, minHeight: 80 }} />
      </Spin>
    </div>
  );
};

export default SsoCallback;
