"use client";

import { useEffect } from "react";

export default function Reset() {
  useEffect(() => {
    localStorage.clear();
    window.location.replace("/login");
  }, []);

  return <p style={{textAlign:"center",marginTop:"40vh",color:"#666"}}>Clearing session...</p>;
}
