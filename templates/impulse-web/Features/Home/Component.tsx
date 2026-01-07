// Generated interface - matches Props.cs
interface HomeProps {
  title: string;
  message: string;
  generatedAt: string;
}

export function Home({ title, message, generatedAt }: HomeProps) {
  return (
    <div className="home">
      <h1>{title}</h1>
      <p>{message}</p>
      <p className="timestamp">
        Generated at: {new Date(generatedAt).toLocaleString()}
      </p>
    </div>
  );
}
