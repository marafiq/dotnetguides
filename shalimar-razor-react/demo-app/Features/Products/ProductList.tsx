import React from 'react';
import { ProductCard } from './ProductCard';


export interface ProductListProps {
  Title?: string;
  Products: Product[];
}

export function ProductList({ Title, Products }: ProductListProps) {
  return (
    <div className="product-list">
      <h2 style={{marginBottom: '24px', fontSize: '28px', color: '#333'}}>{Title}</h2>
    
      {(Products?.Length === 0) && (
          <p style={{color: '#666', textAlign: 'center', padding: '40px'}}>No products available.</p>
        )}
    
      <div style={{display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: '24px'}}>
        {Products.map((product, index) => (
          <ProductCard
            Name={product.Name}
            Description={product.Description}
            ImageUrl={product.ImageUrl}
            Price={product.Price}
            InStock={product.InStock} />
        ))}
      </div>
    </div>
  );
}