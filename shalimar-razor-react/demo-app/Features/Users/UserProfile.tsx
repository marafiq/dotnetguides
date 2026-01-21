import React from 'react';

export interface UserProfileProps {
  Name: string;
  Email: string;
  AvatarUrl: string;
  IsVerified: boolean;
}

export function UserProfile({ Name, Email, AvatarUrl, IsVerified }: UserProfileProps) {
  return (
    <div className="user-profile" style={{display: 'flex', alignItems: 'center', gap: '16px', padding: '20px', background: 'white', borderRadius: '12px', boxShadow: '0 2px 8px rgba(0,0,0,0.1)'}}>
      <img
        src={AvatarUrl}
        alt={Name}
        style={{width: '80px', height: '80px', borderRadius: '50%', objectFit: 'cover'}} />
    
      <div>
        <h3 style={{margin: '0 0 4px 0', fontSize: '20px', color: '#333'}}>{Name}</h3>
        <p style={{margin: '0 0 8px 0', color: '#666'}}>{Email}</p>
    
        {(IsVerified) && (
          <span style={{background: '#e3f2fd', color: '#1565c0', padding: '4px 12px', borderRadius: '20px', fontSize: '12px'}}>Verified</span>
        )}
      </div>
    </div>
  );
}