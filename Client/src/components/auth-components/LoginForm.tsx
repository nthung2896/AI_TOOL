import authService from "@/services/auth/auth.service";
import { setLogin } from "@/store/auth/AuthSlice";
import { setIsLoading, setShowMessage } from "@/store/general/GeneralSlice";
import { useSelector } from "@/store/hooks";
import { AppDispatch } from "@/store/store";
import { LoginViewModelType } from "@/types/appUser/request";
import { LockOutlined, UserOutlined } from "@ant-design/icons";
import { Alert, Button, Form, Input } from "antd";
import { useRouter } from "next/navigation";
import React, { useEffect, useState } from "react";
import { useDispatch } from "react-redux";

const LoginForm: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const route = useRouter();
  const [form] = Form.useForm();
  const loading = useSelector((state) => state.general.isLoading);
  const showMessage = useSelector((state) => state.general.showMessage);
  const [message, setMessage] = useState<string>("");

  const hideAuthMessage = () => {
    dispatch(setShowMessage(false));
  };

  const onLogin = async (loginForm: LoginViewModelType) => {
    dispatch(setIsLoading(true));
    try {
      const data = await authService.login(loginForm);
      if (data != null && data.status) {
        dispatch(setLogin(data));
        const redirectUrl = localStorage.getItem("redirect_url");
        if (redirectUrl) {
          route.push(redirectUrl);
          localStorage.removeItem("redirect_url");
        } else {
          route.push("/dashboard");
        }
      } else {
        setMessage(data.message || "Tài khoản hoặc mật khẩu không chính xác");
        dispatch(setShowMessage(true));
      }
      dispatch(setIsLoading(false));
    } catch (err) {
      setMessage(err instanceof Error ? err.message : "Không thể đăng nhập. Vui lòng kiểm tra kết nối API và thử lại.");
      dispatch(setShowMessage(true));
      dispatch(setIsLoading(false));
    }
  };

  useEffect(() => {
    if (showMessage) {
      const timer = setTimeout(() => hideAuthMessage(), 3000);
      return () => {
        clearTimeout(timer);
      };
    }
  }, []);

  return (
    <>
      {showMessage && (
        <div className="mb-4">
          <Alert
            type="error"
            showIcon
            message={message}
            className="rounded-lg"
          />
        </div>
      )}

      <Form<LoginViewModelType>
        layout="vertical"
        name="login-form"
        form={form}
        onFinish={onLogin}
        requiredMark={false}
      >
        <Form.Item
          name="username"
          label={
            <span className="text-sm font-semibold text-gray-700">
              Tài khoản
            </span>
          }
          rules={[
            { required: true, message: "Vui lòng nhập tài khoản đăng nhập" },
          ]}
        >
          <Input
            prefix={<UserOutlined style={{ color: "#2563eb" }} />}
            placeholder="Nhập tên đăng nhập"
            size="large"
            className="rounded-lg"
          />
        </Form.Item>

        <Form.Item
          name="password"
          label={
            <span className="text-sm font-semibold text-gray-700">
              Mật khẩu
            </span>
          }
          rules={[{ required: true, message: "Vui lòng nhập mật khẩu" }]}
        >
          <Input.Password
            prefix={<LockOutlined style={{ color: "#2563eb" }} />}
            placeholder="Nhập mật khẩu"
            size="large"
            className="rounded-lg"
          />
        </Form.Item>

        <Form.Item className="mb-0 mt-6">
          <Button
            type="primary"
            htmlType="submit"
            block
            loading={loading}
            size="large"
            style={{
              background: "linear-gradient(135deg, #2563eb, #4f46e5)",
              border: "none",
              borderRadius: 10,
              height: 48,
              fontWeight: 700,
              fontSize: 15,
              letterSpacing: "0.03em",
              boxShadow: "0 4px 16px rgba(27,61,228,0.3)",
            }}
          >
            Đăng nhập
          </Button>
        </Form.Item>
      </Form>
    </>
  );
};

export default LoginForm;


