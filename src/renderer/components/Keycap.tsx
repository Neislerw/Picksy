import React from 'react';
import '../styles/Keycap.css';

interface KeycapProps {
  children: React.ReactNode;
  size?: 'sm' | 'md' | 'lg';
}

const Keycap: React.FC<KeycapProps> = ({ children, size = 'md' }) => {
  return (
    <span className={`keycap keycap--${size}`}>{children}</span>
  );
};

export default Keycap;


