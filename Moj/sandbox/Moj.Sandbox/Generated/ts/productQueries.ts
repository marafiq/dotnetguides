import { useQuery, useMutation, useQueryClient, QueryClient, QueryClientProvider, queryOptions } from '@tanstack/react-query';

//  Type Definitions

export interface Product {
  id: string;
  name: string;
  description: string;
  price: number;
  category: string;
  tags: string[];
  inStock: boolean;
}

export interface ProductsResponse {
  products: Product[];
  total: number;
  page: number;
  pageSize: number;
  hasNextPage: boolean;
}

export interface CreateProductInput {
  name: string;
  description: string;
  price: number;
  category: string;
  tags?: string[];
}

export interface UpdateProductInput {
  name?: string;
  description?: string;
  price?: number;
  category?: string;
  tags?: string[];
  inStock?: boolean;
}

// Placeholder API (would be imported in real app)
declare const api: {
  products: {
    list: (filters: ProductFilters) => Promise<ProductsResponse>;
    get: (id: string) => Promise<Product>;
    byCategory: (category: string) => Promise<Product[]>;
    create: (input: CreateProductInput) => Promise<Product>;
    update: (id: string, data: UpdateProductInput) => Promise<Product>;
    delete: (id: string) => Promise<void>;
  };
};

declare const toast: {
  success: (message: string) => void;
  error: (message: string) => void;
};

interface ProductFilters {
  category?: string;
  page?: number;
  pageSize?: number;
  sort?: string;
  q?: string;
}

//  Query Client

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000,
      gcTime: 10 * 60 * 1000,
      retry: 3,
      refetchOnWindowFocus: false
    }
  }
});

//  Query Options Factories

export const productsQueryOptions = (filters: ProductFilters) =>
  queryOptions({
    queryKey: ['products', filters] as const,
    queryFn: async () => api.products.list(filters),
    staleTime: 5 * 60 * 1000
  });

export const productQueryOptions = (productId: string) =>
  queryOptions({
    queryKey: ['product', productId] as const,
    queryFn: async () => api.products.get(productId),
    staleTime: 10 * 60 * 1000
  });

export const productsByCategoryOptions = (category: string) =>
  queryOptions({
    queryKey: ['products', 'category', category] as const,
    queryFn: async () => api.products.byCategory(category),
    staleTime: Infinity
  });

//  Mutations

export const createProductMutation = {
  mutationKey: ['createProduct'] as const,
  mutationFn: async (input: CreateProductInput) => api.products.create(input),
  onSuccess: async () => {
    await queryClient.invalidateQueries({ queryKey: ['products'] });
    toast.success('Product created!');
  },
  onError: (error: Error) => {
    toast.error(error.message);
  }
};

export const updateProductMutation = {
  mutationKey: ['updateProduct'] as const,
  mutationFn: async ({ id, data }: { id: string; data: UpdateProductInput }) =>
    api.products.update(id, data),
  onMutate: async (variables: { id: string; data: UpdateProductInput }) => {
    // Cancel outgoing queries
    await queryClient.cancelQueries({ queryKey: ['product', variables.id] });

    // Snapshot previous value
    const previousProduct = queryClient.getQueryData<Product>(['product', variables.id]);

    // Optimistically update
    queryClient.setQueryData<Product>(['product', variables.id], (old) =>
      old ? { ...old, ...variables.data } : undefined
    );

    return { previousProduct };
  },
  onError: (
    _error: Error,
    variables: { id: string; data: UpdateProductInput },
    context: { previousProduct: Product | undefined } | undefined
  ) => {
    // Rollback on error
    if (context?.previousProduct) {
      queryClient.setQueryData(['product', variables.id], context.previousProduct);
    }
  },
  onSettled: async (_data: unknown, _error: unknown, variables: { id: string }) => {
    // Refetch to ensure consistency
    await queryClient.invalidateQueries({ queryKey: ['product', variables.id] });
    await queryClient.invalidateQueries({ queryKey: ['products'] });
  }
};

export const deleteProductMutation = {
  mutationKey: ['deleteProduct'] as const,
  mutationFn: async (productId: string) => api.products.delete(productId),
  onSuccess: async () => {
    await queryClient.invalidateQueries({ queryKey: ['products'] });
  }
};

//  Custom Hooks

export const useProducts = (filters: ProductFilters) => {
  return useQuery(productsQueryOptions(filters));
};

export const useProduct = (productId: string) => {
  return useQuery(productQueryOptions(productId));
};

export const useCreateProduct = () => {
  return useMutation(createProductMutation);
};

export const useUpdateProduct = () => {
  return useMutation(updateProductMutation);
};

export const useDeleteProduct = () => {
  return useMutation(deleteProductMutation);
};
