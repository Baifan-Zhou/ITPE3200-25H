const API_URL = import.meta.env.VITE_API_URL;

const getAuthHeaders = () => {
  const token = localStorage.getItem('token');
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
  };
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }
  return headers;
};

const handleResponse = async (response: Response) => {
  if (response.ok) {  // HTTP status code success 200-299
    if (response.status === 204) { // Detele returns 204 No content
      return null;
    }
    return response.json(); // other returns response body as JSON
  } else {
    const errorText = await response.text();
    throw new Error(errorText || 'Network response was not ok');
  }
};

// Get itemlist
export const fetchItems = async () => {
  const response = await fetch(`${API_URL}/api/itemapi/itemlist`);
  return handleResponse(response);
};
// Get item by id
export const fetchItemById = async (itemId: string) => {
  const response = await fetch(`${API_URL}/api/itemapi/${itemId}`);
  return handleResponse(response);
};
// Post create item
export const createItem = async (item: any) => {
  const response = await fetch(`${API_URL}/api/itemapi/create`, {
    method: 'POST',
    headers: getAuthHeaders(), // Use the new helper here
    body: JSON.stringify(item),
  });
  return handleResponse(response);
};
// Put update item
export const updateItem = async (itemId: number, item: any) => {
  const response = await fetch(`${API_URL}/api/itemapi/update/${itemId}`, {
    method: 'PUT',
    headers: getAuthHeaders(), // Use the new helper here
    body: JSON.stringify(item),
  });
  return handleResponse(response);
};
// Delete item
export const deleteItem = async (itemId: number) => {
  const response = await fetch(`${API_URL}/api/itemapi/delete/${itemId}`, {
    method: 'DELETE',
    headers: getAuthHeaders(), // Use the new helper here
  });
  return handleResponse(response);
};
