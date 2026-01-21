import React from 'react';

export interface ProductCardProps {
  Name: string;
  Description: string;
  ImageUrl: string;
  Price: number;
  InStock?: boolean;
}

export function ProductCard({ Name, Description, ImageUrl, Price, InStock }: ProductCardProps) {
  return (
    <div className="product-card" style={{border: '1px solid #e0e0e0', borderRadius: '12px', padding: '20px', background: 'white', boxShadow: '0 2px 8px rgba(0,0,0,0.1)'}}>
      <img src={ImageUrl} alt={Name} style={{width: '100%', height: '200px', objectFit: 'cover', borderRadius: '8px'}} />
    
      <h3 style={{margin: '16px 0 8px 0', fontSize: '18px', color: '#333'}}>{Name}</h3>
    
      <p style={{color: '#666', fontSize: '14px', marginBottom: '12px'}}>{Description}</p>
    
      <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center'}}>
        <span style={{fontSize: '24px', fontWeight: 'bold', color: '#0066cc'}}>${Price}</span>
    
        {(InStock) && (
          <span style={{background: '#e8f5e9', color: '#2e7d32', padding: '4px 12px', borderRadius: '20px', fontSize: '12px'}}>In Stock</span>
        )}
      </div>
    
      <button style={{width: '100%', marginTop: '16px', padding: '12px', background: '#0066cc', color: 'white', border: 'none', borderRadius: '8px', fontSize: '16px', cursor: 'pointer'}}>
        Add to Cart
      </button>
    </div>
  );
}