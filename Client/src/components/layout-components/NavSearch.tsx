import { SearchOutlined } from "@ant-design/icons";
import { Input } from "antd";
import React, { useState } from "react";

const NavSearch = () => {
  const [searchValue, setSearchValue] = useState("");

  return (
    <Input
      style={{ width: 240, height: 36 }}
      placeholder="Tìm kiếm..."
      value={searchValue}
      onChange={(e) => setSearchValue(e.target.value)}
      suffix={
        <SearchOutlined className="cursor-pointer text-gray-400 hover:text-blue-600" />
      }
    />
  );
};

export default NavSearch;
